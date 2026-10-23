# CrateWatch — Context

## Glossary

**Artist** — a musical act the user tracks. Created manually by user (name lookup against MusicBrainz).

**Release** — one album, EP or other release by an Artist, deduped by title only. Pressing/version are NOT tracked — a reissue/remaster of the same title is the same Release, not a separate one. Sourced from MusicBrainz release-groups on import; studio albums are imported when the Artist is added, other types on demand (see decisions log). Carries a `firstReleaseDate` (nullable, normalized to a full date — MusicBrainz's own year-only/year-month precision isn't tracked separately, see decisions log) for display/sorting; this does not affect title-based dedup.

**Release Type** — classification derived from the MusicBrainz release-group type (Album, EP, Single, Compilation, Live, Soundtrack, etc). `STUDIO_ALBUM` is imported when an Artist is added; EP, COMPILATION, LIVE, SOUNDTRACK, SINGLE and OTHER are imported per type the first time the user switches that type on in the Artist page.

**User** — an account. Artists and Releases are a shared catalog (see ADR [0002](docs/adr/0002-shared-artist-release-catalog.md)) — a User "tracks" an Artist rather than owning a private copy of it; two Users tracking the same real-world artist share the same imported Artist/Release rows, not separate copies.

**Release state** — `NONE`, `WISHLIST`, `OWNED`, or `DONT_WANT`, scoped per `(User, Release)` rather than a field on the Release itself (Releases are shared, state isn't). Mutually exclusive — moving a Release to `OWNED` clears `WISHLIST` automatically. `DONT_WANT` is an explicit rejection (distinct from `NONE`, which just means no decision made yet).

**ShopLink** — a URL (+ optional label) a User attaches to a Release, pointing at a webshop page for that item. Per-User even though the Release is shared — a shop watch is personal, not visible to other Users tracking the same Release. Placeholder for future daily availability-checking; no checking logic exists yet.

## Open decisions log
- Release identity: dedupe by title only, no year/version (2026-07-27)
- Reversed 2026-10-09: Release now stores a first-release-date for display/sorting (dedupe-by-title unchanged). MusicBrainz's partial-precision dates (year-only / year-month / full date) are normalized to a best-effort full date (missing month/day defaulted to 1), not tracked as a separate precision field.
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
- Renamed cd-watcher -> CrateWatch (2026-10-12): scope is broadening beyond CDs — vinyl LPs are planned (not yet modeled/built), on top of the already-modeled ShopLink store-watching concept. "CD Watcher" no longer fit.
- Reversed 2026-10-12 (ADR 0002, supersedes ADR 0001): Artist/Release catalog is now shared, not per-user private. Adding an Artist another User already tracks links to the existing data instead of re-importing. Release state and ShopLinks moved to per-user tables since they can no longer live on the now-shared Release row.
- Reversed 2026-10-20 (supersedes "Import scope" of 2026-07-27): import keeps only studio albums (primary type Album, no secondary types). EPs, singles, compilations, live, soundtracks and others are skipped at import. Releases imported earlier keep their original types.
- Refined 2026-10-21 (builds on 2026-10-20): studio albums are still the only type imported at add time, but the Artist page has a switch per other type (EP, Compilation, Live, Soundtrack, Single, Other). Switching one on makes the backend fetch just that type from MusicBrainz, store it, and return it; the backend remembers (in memory, 24h) which types it already fetched per Artist. C# backend only; the Java backend still imports studio albums only.
- Reversed 2026-10-21 (supersedes the 2026-10-05 open self-registration): there is no register endpoint or screen. Accounts are created by the admin on the server with `dotnet cratewatcher.dll create-user <name>` (C# backend). Login is rate limited (5/minute per client IP) and the login token lasts 7 days. Production runs only the C# backend at https://cdwatcher.verkerkdigital.nl behind NPMplus (see DEPLOY.md).
- Added 2026-10-22: **Shop** (per-User: name + search URL template with `{query}`) and **ShopAvailability** (last scan result per Shop and Release). A manual "Scan wishlist" on the Availability page fetches each Shop's search page for every wishlist Release and looks for a link whose text contains the title with the artist nearby; no price, no format (CD/vinyl) check, no JavaScript-rendered shops, no scheduled scan. Fetches only connect to public addresses. The older `shop_links` table is still unused.
- Decided 2026-10-22: keep both backends, Java/Micronaut (`cratewatch-server/`) and C#/ASP.NET Core (`cratewatch-server-c/`). Production runs the C# one; the Java one stays for testing and comparison and has not been given the newer features (Collection/Wishlist state, per-type import, create-user, login rate limit). The frontend sends the artist name on create so it works with both.
- Added 2026-10-22: adding a Shop now needs an example album (artist + title). The backend searches the shop for it and checks it shows up; if the shop answers with JSON it remembers where the products are (`json_items_path`, `json_url_field` on the Shop), otherwise it keeps looking for links in the HTML. Scans pick JSON or HTML per answer. GET only, no custom headers; shops that need a POST, headers, or build their page with JavaScript are still not supported. Shops added before this have no JSON rule and are scanned as HTML only.
