# Authenticated user search

## Problem

Authenticated application flows need a focused way to search users and receive the stable backend user id. The public global search endpoint is intentionally mixed-purpose navigation search and omits `userId` by design, so workflows such as team invitations must not depend on it.

## Required contract

Add authenticated, privacy-safe filtering on the existing users collection route:

`GET /v1/lan/users?query={query}&cursor={cursor}&pageSize={pageSize}`

The response uses the same pagination envelope as global search:

- `results`: bounded user search results.
- `nextCursor`: continuation cursor when more matches exist, otherwise `null`.
- `hasMore`: whether another page is available.

Each result includes:

- `id`: stable backend `Guid` user id.
- `type`: `user`.
- `username`: public username.
- `displayLabel`: selection label, initially the username.
- `supportingText`: optional helper text, initially `User`.

The endpoint must not expose private account fields such as email, Auth0 id, roles, deletion state, timestamps, or first/last name. It should return only active users with a usable username. Team invite flows can use the returned `id`, but the existing invite endpoint remains responsible for final team membership, pending invite, captain, and cooldown rules.

## Notes

This search is authenticated because it returns stable user ids for app workflows, even though the displayed user fields are public-safe. It is intentionally separate from global search so navigation search can remain public and id-free.

The database must include endpoint-specific indexes for this access pattern: a PostgreSQL trigram index on active users with usernames for username matching, and a B-tree cursor index on normalized username plus id for deterministic page continuation.
