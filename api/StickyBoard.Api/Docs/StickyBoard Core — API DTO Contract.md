# ✅ StickyBoard Core — Final API DTO Contract

Version `v1-core`
 *Source of truth. Backend-first. No Swift here.*

------

## ✅ Permissions

### PermissionDto

```
{
  "userId": "string",
  "boardId": "string",
  "role": "owner | editor | commenter | viewer",
  "grantedAt": "ISO8601"
}
```

### GrantPermissionDto

```
{
  "userId": "string",
  "role": "owner | editor | commenter | viewer"
}
```

### UpdatePermissionDto

```
{
  "role": "owner | editor | commenter | viewer"
}
```

------

## ✅ Direct Messages

### MessageDto

```
{
  "id": "string",
  "senderId": "string | null",
  "receiverId": "string",
  "subject": "string | null",
  "body": "string",
  "type": "invite | system | direct | org_invite",
  "relatedBoardId": "string | null",
  "relatedOrgId": "string | null",
  "status": "unread | read | archived",
  "createdAt": "ISO8601"
}
```

### SendMessageDto

```
{
  "receiverId": "string",
  "subject": "string | null",
  "body": "string",
  "type": "invite | system | direct | org_invite",
  "relatedBoardId": "string | null",
  "relatedOrgId": "string | null"
}
```

### UpdateMessageStatusDto

```
{
  "status": "unread | read | archived"
}
```

------

## ✅ Users & Auth

### UserDto

```
{
  "id": "string",
  "email": "string",
  "displayName": "string",
  "avatarUrl": "string | null",
  "role": "user | admin | moderator"
}
```

### UserSelfDto

```
{
  "id": "string",
  "email": "string",
  "displayName": "string",
  "avatarUrl": "string | null",
  "prefs": {},
  "createdAt": "ISO8601"
}
```

### UserUpdateDto

```
{
  "displayName": "string",
  "avatarUrl": "string | null",
  "prefs": {}
}
```

### ChangePasswordDto

```
{
  "oldPassword": "string",
  "newPassword": "string"
}
```

### AuthLoginRequest

```
{
  "email": "string",
  "password": "string"
}
```

### AuthLoginResponse

```
{
  "accessToken": "string",
  "refreshToken": "string",
  "user": { /* UserSelfDto */ }
}
```

### AuthRefreshRequest

```
{
  "refreshToken": "string"
}
```

### AuthRefreshResponse

```
{
  "accessToken": "string",
  "refreshToken": "string"
}
```

### RegisterRequestDto

```
{
  "email": "string",
  "password": "string",
  "displayName": "string",
  "inviteToken": "string | null"
}
```

### RegisterResponseDto

```
{
  "accessToken": "string",
  "refreshToken": "string",
  "user": { /* UserSelfDto */ }
}
```

------

## ✅ Organizations

### OrganizationDto

```
{
  "id": "string",
  "name": "string",
  "ownerId": "string"
}
```

### OrganizationCreateDto

```
{
  "name": "string"
}
```

### OrganizationUpdateDto

```
{
  "name": "string"
}
```

### OrganizationMemberDto

```
{
  "user": { /* UserDto */ },
  "role": "owner | admin | moderator | member | guest"
}
```

------

## ✅ Board Folders

### BoardFolderDto

```
{
  "id": "string",
  "name": "string",
  "orgId": "string | null",
  "userId": "string | null",
  "icon": "string | null",
  "color": "string | null",
  "meta": {}
}
```

### BoardFolderCreateDto

```
{
  "name": "string",
  "orgId": "string | null",
  "icon": "string | null",
  "color": "string | null",
  "meta": {}
}
```

### BoardFolderUpdateDto

```
{
  "name": "string | null",
  "icon": "string | null",
  "color": "string | null",
  "meta": {}
}
```

------

## ✅ Boards

### BoardDto

```
{
  "id": "string",
  "title": "string",
  "visibility": "private_ | shared | public_",
  "ownerId": "string",
  "orgId": "string | null",
  "folderId": "string | null",
  "theme": {},
  "meta": {},
  "createdAt": "ISO8601",
  "updatedAt": "ISO8601"
}
```

### BoardCreateDto

```
{
  "title": "string",
  "visibility": "private_ | shared | public_",
  "orgId": "string | null",
  "folderId": "string | null",
  "theme": {},
  "meta": {}
}
```

### BoardUpdateDto

```
{
  "title": "string | null",
  "visibility": "private_ | shared | public_ | null",
  "folderId": "string | null",
  "theme": {},
  "meta": {}
}
```

------

## ✅ Tabs

### TabDto

```
{
  "id": "string",
  "boardId": "string",
  "title": "string",
  "tabType": "board | calendar | timeline | kanban | whiteboard | chat | metrics | custom",
  "position": 0,
  "layout": {}
}
```

### TabCreateDto

```
{
  "boardId": "string",
  "title": "string",
  "tabType": "board | calendar | timeline | kanban | whiteboard | chat | metrics | custom",
  "position": 0,
  "layout": {}
}
```

### TabUpdateDto

```
{
  "title": "string | null",
  "tabType": "string | null",
  "position": 0,
  "layout": {}
}
```

------

## ✅ Sections

### SectionDto

```
{
  "id": "string",
  "tabId": "string",
  "parentSectionId": "string | null",
  "title": "string",
  "position": 0,
  "layout": {}
}
```

### SectionCreateDto

```
{
  "tabId": "string",
  "parentSectionId": "string | null",
  "title": "string",
  "position": 0,
  "layout": {}
}
```

### SectionUpdateDto

```
{
  "title": "string | null",
  "position": 0,
  "parentSectionId": "string | null",
  "layout": {}
}
```

------

## ✅ Cards

### CardDto

```
{
  "id": "string",
  "boardId": "string",
  "tabId": "string",
  "sectionId": "string | null",
  "type": "note | task | event_ | drawing",
  "title": "string | null",
  "content": {},
  "tags": ["string"],
  "status": "open | in_progress | blocked | done | archived",
  "priority": 0,
  "assigneeId": "string | null",
  "dueDate": "ISO8601 | null",
  "startTime": "ISO8601 | null",
  "endTime": "ISO8601 | null",
  "updatedAt": "ISO8601"
}
```

### CardCreateDto

```
{
  "boardId": "string",
  "tabId": "string",
  "sectionId": "string | null",
  "type": "note | task | event_ | drawing",
  "title": "string | null",
  "content": {},
  "tags": ["string"] | null,
  "priority": 0,
  "assigneeId": "string | null",
  "dueDate": "ISO8601 | null"
}
```

### CardUpdateDto

```
{
  "title": "string | null",
  "content": {},
  "tags": ["string"] | null,
  "status": "open | in_progress | blocked | done | archived | null",
  "priority": 0,
  "assigneeId": "string | null",
  "dueDate": "ISO8601 | null",
  "startTime": "ISO8601 | null",
  "endTime": "ISO8601 | null",
  "sectionId": "string | null",
  "tabId": "string | null"
}
```

------

## ✅ Card Comments

### CardCommentDto

```
{
  "id": "string",
  "cardId": "string",
  "user": { /* UserDto */ },
  "content": "string",
  "createdAt": "ISO8601"
}
```

### CardCommentCreateDto

```
{
  "content": "string"
}
```

------

## ✅ Board Chat

### BoardMessageDto

```
{
  "id": "string",
  "boardId": "string",
  "user": { /* UserDto */ },
  "content": "string",
  "createdAt": "ISO8601"
}
```

### BoardMessageCreateDto

```
{
  "content": "string"
}
```

------

## ✅ Social — User Relations

### UserRelationDto

```
{
  "userId": "string",
  "friendId": "string",
  "status": "active_ | blocked | inactive",
  "createdAt": "ISO8601",
  "updatedAt": "ISO8601"
}
```

### UserRelationCreateDto

```
{
  "friendId": "string"
}
```

### UserRelationUpdateDto

```
{
  "status": "active_ | blocked | inactive"
}
```

------

## ✅ Invites

### InviteDto

```
{
  "id": "string",
  "email": "string",
  "boardId": "string | null",
  "orgId": "string | null",
  "boardRole": "string | null",
  "orgRole": "string | null",
  "accepted": false,
  "expiresAt": "ISO8601"
}
```

### InviteCreateDto

```
{
  "email": "string",
  "boardId": "string | null",
  "orgId": "string | null",
  "boardRole": "string | null",
  "orgRole": "string | null",
  "expiresInDays": 7
}
```

### InviteCreateResponseDto

```
{
  "id": "string",
  "token": "string",
  "expiresAt": "ISO8601"
}
```

### InviteRedeemRequestDto

```
{
  "token": "string"
}
```

### InviteListItemDto

```
{
  "id": "string",
  "email": "string",
  "boardId": "string | null",
  "organizationId": "string | null",
  "boardRole": "string | null",
  "orgRole": "string | null",
  "accepted": false,
  "createdAt": "ISO8601",
  "expiresAt": "ISO8601",
  "senderDisplayName": "string"
}
```

### InvitePublicDto

```
{
  "email": "string",
  "boardId": "string | null",
  "organizationId": "string | null",
  "boardRole": "string | null",
  "orgRole": "string | null",
  "accepted": false,
  "expiresAt": "ISO8601",
  "senderDisplayName": "string"
}
```

------

## ✅ Response Envelopes

### ApiResponseDto<T>

```
{
  "success": true,
  "message": "string | null",
  "data": {} 
}
```

### ErrorDto

```
{
  "code": "string",
  "message": "string",
  "details": "string | null"
}
```

------

## ✅ HTTP Examples

Success:

```
200
{
  "success": true,
  "data": { "id": "uuid" }
}
```

Error:

```
422
{
  "code": "VALIDATION_ERROR",
  "message": "Title is required"
}
```