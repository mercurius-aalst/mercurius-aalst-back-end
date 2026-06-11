## Overview

Add authenticated filtering to the users collection route. Search returns only public-safe user selection data plus the stable backend user id required by authenticated application workflows.

## API

Route:

`GET /v1/lan/users?query={query}&cursor={cursor}&pageSize={pageSize}`

Response:

```json
{
  "results": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "type": "user",
      "username": "PlayerOne",
      "displayLabel": "PlayerOne",
      "supportingText": "User"
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

The route uses the existing `/lan/users` collection resource. Supplying `query` returns privacy-safe search results to authenticated callers; omitting `query` preserves the existing admin-only list-all behavior.

## Filtering

The search uses normalized username matching and follows public user visibility rules:

- exclude deleted users;
- exclude users without a username or normalized username;
- match case-insensitively;
- return an empty list for very short queries.

The endpoint does not pre-enforce workflow-specific rules such as team captainship, duplicate pending invites, existing membership, or cooldowns. Those remain enforced by their existing workflow mutations.

## Bounds

Use the existing search request limits for minimum query length, maximum query length, maximum cursor length, default page size, and maximum page size. Results are ordered deterministically by exact username match, prefix match, contains match, normalized username, and user id. Continuation uses an opaque keyset cursor compatible with the current query, mirroring global search behavior.

## Indexes

Authenticated user search reuses the existing PostgreSQL trigram index on `Users.NormalizedUsername` that supports exact, prefix, and contains matching for active users. The existing unique normalized-username index and bounded page size are sufficient for deterministic cursor ordering by normalized username and user id, so this change does not add a dedicated migration.

## Privacy

The DTO intentionally omits email, email verification state, Auth0 id, roles, deleted state, first name, last name, platform ids, and timestamps. `displayLabel` mirrors `username` so no additional profile fields are introduced.
