# 0001 - Per-user private catalog, not shared

Date: 2026-07-28

## Status

Accepted

## Context

App is multi-user with login. Need to decide whether Artist/Release data is:

- shared globally (imported once, all Users toggle their own Wishlist/Owned state on the same rows), or
- private per User (each User's Artists/Releases are entirely their own, even if two Users track the same real-world artist)

A shared catalog avoids duplicate MusicBrainz imports and duplicate storage when multiple Users track the same artist. A private catalog duplicates that data per User but keeps ownership simple and avoids cross-user coupling.

## Decision

Each User has a fully private catalog. Artists and Releases are not shared entities — importing an artist under one User's account has no effect on any other User. Two Users tracking the same artist each trigger their own MusicBrainz import and get their own copy of that Artist's Releases.

## Consequences

- No risk of one User's data (wishlist edits, manual releases, future ShopLinks) leaking into another's view — ownership model stays trivial (every row scoped by user_id, no join table needed for state).
- Duplicate MusicBrainz imports and duplicate rows when multiple Users track the same artist. Acceptable at this app's expected scale (a few personal users), traded off against simplicity now.
- Reversing this later (moving to a shared catalog) means merging duplicate Artist/Release rows across Users and introducing a join table for per-user state — a real migration, not a quick config flip.
