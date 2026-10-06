# CD Watcher — Context

## Glossary

**Artist** — a musical act the user tracks. Created manually by user (name lookup against MusicBrainz).

**Release** — one album or EP by an Artist, deduped by title only. Year, pressing, and version are NOT tracked — a reissue/remaster of the same title is the same Release, not a separate one. Sourced from MusicBrainz release-groups on import.

**Release Type** — classification imported from MusicBrainz release-group type (Album, EP, Single, Compilation, Live, Soundtrack, etc). All types are imported and stored; the user filters/hides types in the UI afterward rather than choosing at import time.

**User** — an account. Owns a private catalog: their own Artists and Releases are not shared with other Users, even if two Users track the same real-world artist (each gets their own imported copy).

**Release state** — single field per Release: `NONE`, `WISHLIST`, `OWNED`, or `DONT_WANT`. Mutually exclusive — moving a Release to `OWNED` clears `WISHLIST` automatically. `DONT_WANT` is an explicit rejection (distinct from `NONE`, which just means no decision made yet).

**ShopLink** — a URL (+ optional label) attached to a Release, pointing at a webshop page for that item. Placeholder for future daily availability-checking; no checking logic exists yet.

## Open decisions log
- Release identity: dedupe by title only, no year/version (2026-07-27)
- Import source: MusicBrainz release-group API (2026-07-27)
- Import scope: all release-group types imported, type filtering happens in view, not at import (2026-07-27)
- Release state is mutually exclusive: NONE / WISHLIST / OWNED / DONT_WANT (2026-07-27, DONT_WANT added later)
- Manual Release add supported alongside import (2026-07-27)
- Artist creation: search MusicBrainz, user picks correct match from candidates (2026-07-27)
- Multi-user with login; catalog is per-user private, not shared (2026-07-27)
- Auth: Micronaut Security JWT, admin/manual account creation, no open signup (2026-07-27)
- Reversed 2026-10-05: open self-registration now allowed. Anyone can create a User via a public register screen/endpoint (username + password).
- ShopLink placeholder modeled now: per-Release URL+label, no check logic yet (2026-07-27)
- DB: PostgreSQL (2026-07-27)
- Layout: monorepo, backend/ (Micronaut) + frontend/ (Vue) (2026-07-27)
- Run via docker-compose (postgres + backend + frontend) from day one (2026-07-27)
- MusicBrainz import runs synchronously per Artist, no async job queue (2026-07-27)
- Cover art fetched from Cover Art Archive, shown in UI (2026-07-27)
- Nav: Artist list -> Artist detail (Release grid, type filter, state toggle) + global Wishlist/Owned views (2026-07-27)
