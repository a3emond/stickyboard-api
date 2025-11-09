-- Worker Jobs (generic async queue)
CREATE TABLE IF NOT EXISTS worker_jobs (
  id           bigserial PRIMARY KEY,
  kind         text NOT NULL,                       -- 'mention_notify'|'due_reminder'|'search_index'|'analytics_rollup'|'cdn_gc'|'invite_email'
  payload      jsonb NOT NULL,
  status       text NOT NULL DEFAULT 'queued',      -- 'queued'|'running'|'done'|'dead'
  attempts     int NOT NULL DEFAULT 0,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  available_at timestamptz NOT NULL DEFAULT now(),
  last_error   text
);
CREATE INDEX IF NOT EXISTS ix_jobs_ready ON worker_jobs(status, available_at);

CREATE TABLE IF NOT EXISTS worker_job_attempts (
  id          bigserial PRIMARY KEY,
  job_id      bigint NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
  started_at  timestamptz NOT NULL,
  finished_at timestamptz,
  error       text
);