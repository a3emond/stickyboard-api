# StickyBoard Files & CDN – Architecture and Schema

API: [https://stickyboard.aedev.pro](https://stickyboard.aedev.pro/)
 CDN: [https://cdn.aedev.pro](https://cdn.aedev.pro/)
 Origin root (example): `/srv/cdn`

------

## 1) Database Schema (Fresh Create)

This defines the final schema directly (no ALTERs). Use when recreating DB from scratch.

### 1.1 attachments (original logical file)

```sql
CREATE TABLE attachments (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id    uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  board_id        uuid REFERENCES boards(id) ON DELETE CASCADE,
  card_id         uuid REFERENCES cards(id) ON DELETE SET NULL,

  filename        text NOT NULL,
  mime            text,
  byte_size       bigint,
  checksum_sha256 bytea,
  storage_path    text NOT NULL,
  is_public       boolean NOT NULL DEFAULT false,

  status          text NOT NULL DEFAULT 'ready',
  meta            jsonb NOT NULL DEFAULT '{}'::jsonb,

  uploaded_by     uuid REFERENCES users(id) ON DELETE SET NULL,
  version         int NOT NULL DEFAULT 0,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz
);

CREATE INDEX ix_attach_board     ON attachments(board_id);
CREATE INDEX ix_attach_card      ON attachments(card_id);
CREATE INDEX ix_attach_deleted   ON attachments(deleted_at);
CREATE INDEX ix_attach_workspace ON attachments(workspace_id, created_at DESC);
CREATE INDEX ix_attach_storage   ON attachments(storage_path);

CREATE TRIGGER trg_attach_upd
  BEFORE UPDATE ON attachments
  FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_attach_version
  BEFORE UPDATE ON attachments
  FOR EACH ROW EXECUTE FUNCTION bump_version();
```

### 1.2 attachment_variants (worker‑generated derivatives)

```sql
CREATE TABLE attachment_variants (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  parent_id       uuid NOT NULL REFERENCES attachments(id) ON DELETE CASCADE,

  variant         text NOT NULL,
  mime            text NOT NULL,
  byte_size       bigint,
  width           int,
  height          int,
  duration_ms     int,
  storage_path    text NOT NULL,
  status          text NOT NULL DEFAULT 'ready',
  checksum_sha256 bytea,

  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_variant_unique
  ON attachment_variants(parent_id, variant);

CREATE INDEX ix_variant_parent
  ON attachment_variants(parent_id);
  
-- Safety: disable triggers on attachment_variants (if someone adds one accidentally)
ALTER TABLE attachment_variants DISABLE TRIGGER ALL;

```

### 1.3 file_tokens (revocable signed URL tokens)

```sql
CREATE TABLE file_tokens (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  attachment_id uuid NOT NULL REFERENCES attachments(id) ON DELETE CASCADE,
  variant       text DEFAULT NULL,
  secret        bytea NOT NULL,
  audience      text NOT NULL DEFAULT 'download',
  expires_at    timestamptz NOT NULL,
  created_by    uuid REFERENCES users(id) ON DELETE SET NULL,
  revoked       boolean NOT NULL DEFAULT false,
  created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_file_tokens_valid
  ON file_tokens(attachment_id, expires_at)
  WHERE revoked = false;
```

------

## 2) Responsibilities Split

### 2.1 API ([https://stickyboard.aedev.pro](https://stickyboard.aedev.pro/))

- AuthN/AuthZ, permission checks (workspace/board/card membership)
- CRUD for `attachments` and `attachment_variants` metadata
- Issues **signed URLs** by creating `file_tokens` rows
- Enqueues worker jobs to generate variants (thumbnails, previews)
- Audits access requests (optional)

### 2.2 Worker Service

- Watches job queue (`worker_jobs`)
- On new attachment: generate thumbnails/previews
- Inserts/updates `attachment_variants` with `status="ready"`
- Extracts metadata (EXIF, pages, duration) into `attachments.meta`
- Periodic `file_tokens_gc()` cleanup

### 2.3 CDN Origin ([https://cdn.aedev.pro](https://cdn.aedev.pro/), Apache + PHP)

- Receives **signed URL** requests
- Validates `tid` (token id), `exp` (epoch seconds), `sig` (HMAC)
- Checks DB `file_tokens` (by `tid`): not revoked, not expired, audience ok
- Validates signature, resolves disk path (`/srv/cdn/<storage_path>`) and streams via `X-Sendfile`
- Sets **caching policy**:
  - Private objects: `Cache-Control: private, max-age=0, must-revalidate` (edge cache **bypassed**)
  - Public objects: `Cache-Control: public, max-age=31536000, immutable`

**Why not cache private content at the edge?** Because query-string signatures + cache-key tricks can inadvertently make edge caches serve files without re-validating. Keep private fetches origin-validated.

------

## 3) Signed URL Design

### 3.1 URL shape

```
https://cdn.aedev.pro/protected?path=<url-encoded-storage_path>&tid=<token-id>&exp=<unix-epoch>&sig=<base64url>
```

- `path` — exact `attachments.storage_path` (or the variant’s `storage_path`)
- `tid`  — `file_tokens.id`
- `exp`  — epoch seconds, must match token row `expires_at`
- `sig`  — HMAC-SHA256 of canonical string using the token’s `secret`

### 3.2 Canonical string & signature

```
// canonical string
"path=" + path + "&tid=" + tid + "&exp=" + exp

// signature
sig = base64url( HMAC_SHA256( canonical, secret_bytes ) )
```

### 3.3 Token issuance (server-side)

- API verifies caller has permission to read the attachment
- API inserts `file_tokens` row with random 32 bytes `secret`, `expires_at`
- API returns signed URL as above

**Revocation**: set `revoked=true` on the specific token row; any URL using that `tid` becomes invalid immediately.

------

## 4) CDN Origin Implementation (Apache + PHP)

### 4.1 Apache sketch

```apache
# /etc/apache2/sites-available/cdn.conf (sketch)
<VirtualHost *:443>
  ServerName cdn.aedev.pro
  DocumentRoot /var/www/cdn_public

  # Protected handler
  RewriteEngine On
  RewriteRule ^/protected$ /validate.php [QSA,L]

  # X-Sendfile to stream from outside webroot
  XSendFile on
  XSendFilePath /srv/cdn
</VirtualHost>
```

### 4.2 validate.php (PostgreSQL + sig check)

```php
<?php
// validate.php
ini_set('display_errors', 0);
http_response_code(403);

function base64url_decode($data) { return base64_decode(strtr($data, '-_', '+/')); }
function base64url_encode($data) { return rtrim(strtr(base64_encode($data), '+/', '-_'), '='); }

$path = $_GET['path'] ?? '';
$tid  = $_GET['tid']  ?? '';
$exp  = isset($_GET['exp']) ? intval($_GET['exp']) : 0;
$sig  = $_GET['sig']  ?? '';

if ($path === '' || $tid === '' || $exp <= time()) { exit; }

// Connect to Postgres
$dsn = 'pgsql:host=127.0.0.1;port=5432;dbname=stickyboard;';
$db  = new PDO($dsn, 'sticky_user', 'sticky_pass', [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);

// Fetch token + resolve storage_path and is_public for sanity check
$sql = "SELECT ft.secret, ft.expires_at, ft.revoked,
               a.storage_path, a.is_public
          FROM file_tokens ft
          JOIN attachments a ON a.id = ft.attachment_id
         WHERE ft.id = :tid
         LIMIT 1";
$stmt = $db->prepare($sql);
$stmt->execute([':tid' => $tid]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { exit; }

if ($row['revoked'] || strtotime($row['expires_at']) < time()) { exit; }

// Verify path matches token’s attachment/variant path
if ($path !== $row['storage_path']) { exit; }

$canonical = 'path=' . $path . '&tid=' . $tid . '&exp=' . $exp;
$calc = hash_hmac('sha256', $canonical, $row['secret'], true);
if (!hash_equals(base64url_encode($calc), $sig)) { exit; }

$file = '/srv/cdn/' . $path; // guaranteed to stay under XSendFilePath
if (!is_file($file)) { http_response_code(404); exit; }

// Caching
if ($row['is_public'] === 't') {
  header('Cache-Control: public, max-age=31536000, immutable');
} else {
  header('Cache-Control: private, max-age=0, must-revalidate');
}

// Content-Type best-effort
$finfo = finfo_open(FILEINFO_MIME_TYPE);
$mime  = finfo_file($finfo, $file) ?: 'application/octet-stream';
header('Content-Type: ' . $mime);

// Stream file via X-Sendfile
header('X-Sendfile: ' . $file);
http_response_code(200);
exit;
```

> For **variant URLs**: you may expose `path` of the variant row instead of the original. The token row can include a non-null `variant`, and your API should verify that the requested variant exists before issuing the URL.

------

## 5) API Contract (minimal)

### POST `/attachments`

- Creates metadata row, returns upload instructions (direct upload or multipart form)
- Body: `{ boardId?, cardId?, filename, mime, byteSize }`
- Response: `{ id, storagePath, status, uploadUrl? }`

### POST `/attachments/{id}/tokens`

- Auth required; checks permission chain (workspace → board → card)
- Body: `{ variant?: string, ttlSeconds?: number, audience?: 'download'|'upload' }`
- Creates `file_tokens` row, returns signed CDN URL

**Response**

```json
{
  "url": "https://cdn.aedev.pro/protected?path=boards/200.../att/abc.jpg&tid=...&exp=...&sig=...",
  "expiresAt": "2025-11-09T15:05:00Z"
}
```

### GET `/attachments/{id}`

- Returns metadata (`attachments` + available `attachment_variants`)

### GET `/attachments/{id}/variants`

- Returns list of variant labels available and their dimensions

**Permissions**

- Read = member of workspace/board with read access
- Upload / Delete = editor/owner

------

## 6) Server-side Signature (C#) – Example

```csharp
public static class FileTokenService
{
    private static readonly Base64UrlTextEncoder B64 = new Base64UrlTextEncoder();

    public sealed record SignedUrl(string Url, DateTimeOffset ExpiresAt, Guid TokenId);

    public async Task<SignedUrl> IssueDownloadAsync(Guid attachmentId, string? variant, TimeSpan ttl, UserContext user)
    {
        // 1) permission check (omitted)
        var secret = RandomNumberGenerator.GetBytes(32);
        var tokenId = Guid.NewGuid();
        var exp = DateTimeOffset.UtcNow.Add(ttl);

        // insert file_tokens row
        await _db.ExecuteAsync(@"INSERT INTO file_tokens(id, attachment_id, variant, secret, audience, expires_at, created_by)
                                 VALUES (@id, @att, @var, @sec, 'download', @exp, @usr)",
            new { id = tokenId, att = attachmentId, var = variant, sec = secret, exp = exp.UtcDateTime, usr = user.Id });

        // fetch storage_path (variant preferred if provided)
        var path = await ResolvePathAsync(attachmentId, variant);

        // canonical string
        var canonical = $"path={path}&tid={tokenId}&exp={exp.ToUnixTimeSeconds()}";
        // HMAC
        using var hmac = new HMACSHA256(secret);
        var sig = B64.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        var url = $"https://cdn.aedev.pro/protected?path={Uri.EscapeDataString(path)}&tid={tokenId}&exp={exp.ToUnixTimeSeconds()}&sig={sig}";
        return new SignedUrl(url, exp, tokenId);
    }
}
```

> Store `secret` as `bytea` in `file_tokens`. The PHP validator reads it directly to recompute the HMAC.

------

## 7) Worker: Thumbnail Generation Flow

1. **Trigger**: after upload finalized or via explicit job enqueue
2. **Task**: read original from `/srv/cdn/<storage_path>`
3. Generate variants (e.g., `thumb_64`, `thumb_256`, `web`) under deterministic paths:
   - `boards/<board_id>/att/<id>/thumb_64.webp`
4. Insert `attachment_variants` rows with `status='ready'`
5. Update `attachments.meta` with width/height, EXIF, page count, duration, etc.

**Suggested variant set**

- Images: `thumb_64.webp`, `thumb_256.webp`, `web_1280.webp`
- PDF: `pdf_page_1.webp` (first page), optionally `pdf_page_N`
- Video: poster `thumb_256.webp`, later `video_mp4`

------

## 8) Client Guidance (progressive loading)

- Request metadata (`GET /attachments/{id}`)
- Prefer `thumb_64` or `thumb_256` when available; if not, call token for original but **only** on demand (tap/click)
- Cache signed URLs **in-memory only**; they expire quickly
- Do not persist CDN URLs in your DB; always request a fresh token from API
- Use `ETag`/`Last-Modified` for UI caching of metadata, not for the asset URL

------

## 9) Security Notes

- All private files must be served via `/protected` endpoint and validated at origin
- Never allow Cloudflare to cache private responses by ignoring query strings
- If `is_public = true` on an attachment/variant, you may publish a **public** URL without token and enable long edge caching
- Consider short TTLs (e.g., 5–10 minutes) for signed URLs; clients can refresh seamlessly

------

## 10) Example Storage Paths

```
Original: boards/<board_id>/att/<attachment_id>/original/<filename>
Thumb 64: boards/<board_id>/att/<attachment_id>/thumb_64.webp
Thumb256: boards/<board_id>/att/<attachment_id>/thumb_256.webp
Web 1280: boards/<board_id>/att/<attachment_id>/web_1280.webp
```

Deterministic paths avoid DB lookups during worker writes; the API still returns metadata.

------

