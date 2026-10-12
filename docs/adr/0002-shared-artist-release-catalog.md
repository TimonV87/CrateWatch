# 0002 - Shared Artist/Release catalog, not per-user private

Date: 2026-10-12

## Status

Accepted. Supersedes [0001](0001-per-user-private-catalog.md).

## Context

[0001](0001-per-user-private-catalog.md) chose a private-per-user catalog: every User's Artists/Releases were entirely their own, duplicated on import even when two Users tracked the same real-world artist. That ADR explicitly accepted "duplicate MusicBrainz imports and duplicate rows when multiple Users track the same artist" as the tradeoff for simple ownership.

That tradeoff stopped being acceptable once avoiding re-download became an explicit requirement: adding an artist another User already tracks should not re-hit MusicBrainz/Cover Art Archive or create duplicate `cd_releases` rows.

## Decision

Artists and Releases are now a shared, global catalog:

- `artists`/`cd_releases` are no longer scoped by `user_id` — `artists.musicbrainz_id` is the global dedupe key (`NOT NULL UNIQUE`).
- A new `user_artists` join table (`user_id`, `artist_id`) records which Users track which Artists. Adding an Artist that already exists in the catalog just inserts a `user_artists` row — no MusicBrainz call, no new `cd_releases` rows.
- Per-user facts that can no longer live on the shared rows moved to new per-user tables: `cd_releases.state` (Wishlist/Owned/etc) moved to `user_release_states` (`user_id`, `cd_release_id`, `state`).
- `ShopLinks` (not yet implemented — schema-only placeholder) stay **per-user** even though the Release they point at is shared: a shop watch is personal data, not something to show every User tracking that Release. `shop_links` gained a `user_id` column for this.

## Consequences

- One MusicBrainz import per real-world artist, ever, regardless of how many Users track it. This was the whole point.
- Ownership checks changed shape: "does this User have this Artist" is now `EXISTS` against `user_artists`, not a column comparison on `artists`. Listing a User's Artists/Release-states requires a join (`user_artists`/`user_release_states`) instead of a direct `WHERE user_id = ?`.
- `artists`/`cd_releases` rows now outlive any single User — deleting a User cascades through `user_artists`/`user_release_states` (and `shop_links`), not through the catalog itself. A User deleting "their" artist only ever removes their own link to it, never the shared data (even if they were the only one tracking it) — revisit later if orphaned-artist cleanup turns out to matter.
