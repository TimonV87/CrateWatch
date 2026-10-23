# Open issues

Found in the whole-repo audit of 2026-10-22. Fix one at a time, tick the box when done.
Code references use file + method names because line numbers move.
Everything from the first review (races, username limit, JWT secret, photo cache, etc.) is done and removed from this list.

Status key: `[x]` fixed and tested, `[~]` partly fixed (what is left is written next to it), `[ ]` open.

## Must fix

- [ ] **1. No backup exists yet**
  - Where: `docker-compose.prod.yml` (volume `postgres-data`), `DEPLOY.md` step 10
  - Problem: all data lives in one Docker volume. `docker compose ... down -v` or a lost VM disk deletes every collection and wishlist.
  - Fix (you, on the VM): nightly `pg_dump` cron + Proxmox VM backup, then test one restore. Commands are in `DEPLOY.md` step 10.

## Should fix

- [x] **2. Import read only the first 500 release groups** (`Artists/Services/ArtistImportService.cs`)
  - Fixed: `MaxPages` 5 -> 25 (2,500 release groups, with a `shortcut:` comment). Checked live with Radiohead: Live went from 327 to 387, Singles from 66 to 80. An artist with more than 2,500 release groups is still cut off.
  - Not re-verified: EP (MusicBrainz has 21). That run was cut short by MusicBrainz 503 answers (6 saved). See issue 11.

- [x] **3. Cached old `index.html` could give a blank page after an update** (`cratewatch-web/nginx.conf`)
  - Fixed: `Cache-Control: no-cache` on `index.html` and on SPA routes. Checked with curl.

- [x] **4. Images were never cached by the browser** (`CoverArtController`, `ArtistPhotoController`)
  - Fixed: `Cache-Control: public, max-age=86400` on successful images only (a cached 404 would hide a photo that appears later). Checked with curl.

- [x] **5. Debug logs and unlimited log files in production** (`appsettings.json`, `docker-compose.prod.yml`)
  - Fixed: Debug levels moved to `appsettings.Development.json`; the HTTP client logger is `Warning` otherwise; all three services use `json-file` with `max-size 10m`, `max-file 3`. Checked: 0 Debug request lines in production logs, rotation shown by `docker inspect`.

- [x] **6. Photo download trusted any URL** (`TheAudioDb/ArtistPhotoCacheService.cs`, `Program.cs`)
  - Fixed: only `https` on `theaudiodb.com` or a subdomain; redirects off; 10 s timeout; 5 MB cap. 4 new tests (router IP, other host, look-alike host, plain http).

- [ ] **7. Frontend builds are not repeatable** (`cratewatch-web/Dockerfile`, `package.json`)
  - Problem: Docker copies only `package.json`, so each build resolves the `^` ranges fresh; a new Vuetify/Vite release can change or break the site on update day.
  - Why not done yet: a lockfile made on macOS lacks the Linux rollup binary, and one made in Linux can break `npm install` on the Mac. Needs a lockfile that lists both platforms.
  - Fix idea: generate it with `npm install --package-lock-only --os=linux --os=darwin --cpu=x64 --cpu=arm64` (npm 10.x+), commit, switch the Dockerfile to `npm ci`, test the Docker build and a Mac `npm install`.

- [x] **8. Nothing tested login, ownership or the import flow** (`cratewatcher.Tests/Integration/`)
  - Fixed: 35 integration test cases run the real app in-process on a throwaway Postgres 18 (Testcontainers, needs Docker running). They cover: login right/wrong/unknown, 7-day token, 429 on the 6th login, faked forwarded IPs, no register endpoint, 401 without a token, a deleted user's token, another user's artist/releases/state (404), collection vs wishlist exclusivity, 8 parallel state changes, bad state/search/type/unsearched artist (400). No test calls MusicBrainz.
  - Checked that they can fail: I broke the ownership check in `SetStateAsync`, the one in `FindOwnedArtistAsync`, the login rate limit, and the deleted-user check in turn. Each break failed the matching test(s), then I restored the code.
  - Not covered: the `create-user` command (it reads the terminal; testing it needs a small refactor), and the real import from MusicBrainz.

- [x] **9. Dev compose and README described the old Java setup** (`docker-compose.yml`, `README.md`)
  - Fixed: `docker-compose.yml` is now only a local Postgres 18; the README describes the C# + Vue run steps and links `DEPLOY.md`.

## Nice to have

- [x] **10. Login timing showed which usernames exist** (`Users/Services/AuthService.cs`)
  - Fixed: unknown users are checked against a dummy BCrypt hash. Checked: unknown user 0.109-0.115 s, known user 0.106-0.108 s (first call 0.24 s is warm-up).

- [x] **11. Import did a query and an insert per release, and every switch re-read the whole MusicBrainz list** (`ArtistImportService.cs`, `ReleaseRepository.cs`, `ArtistService.GetReleasesAsync`)
  - Fixed:
    - One query for the artist's known titles, then one `SaveChanges` for all new releases of a run (one transaction: all or nothing), instead of a select and an insert per release.
    - The type filter runs in SQL (`FindByArtistIdAndTypesOrderByFirstReleaseDateAsync`), not in C#.
    - Each MusicBrainz page is kept for 10 minutes. Turning on a second or third type, or reloading, costs no MusicBrainz calls. After a MusicBrainz error, the retry resumes at the page that failed instead of re-reading from page 1. A failed fetch is never cached.
  - Tests (`Integration/ImportTests.cs`, with a fake MusicBrainz): studio import plus EP and Live switches use one MusicBrainz call in total; a duplicate title is stored once; a single 503 is retried and the request succeeds; after 3 failed tries (502) the next request does not re-read page 1 (1 call for page 1, 4 for page 2). I broke the page cache, the duplicate check and the retry in turn; each break failed the matching tests.
  - Limits: the 10-minute page cache is in memory, so a restart empties it (`shortcut:` style, one extra MusicBrainz read). If MusicBrainz keeps answering 503 for longer than 3 tries (about 6 s of waiting), the request still ends in a 502 and the user must try again. Not run against the real MusicBrainz after this change; the earlier 503s there came during heavy testing from one IP.

- [x] **12. MusicBrainz 1 request per second was only respected inside one import** (`MusicBrainz/MusicBrainzClient.cs`, `ArtistsController.Search`)
  - Fixed: one process-wide gate (1.1 s between calls) in `MusicBrainzClient`; up to 3 tries with a 2 s / 4 s pause on a 503; blank search query returns 400 (checked live). Still process-wide only (`shortcut:` comment).

- [~] **13. No browser-hardening headers** (`cratewatch-web/nginx.conf`)
  - Fixed: `X-Content-Type-Options: nosniff`, `Referrer-Policy: same-origin`, `X-Frame-Options: DENY` on the HTML page.
  - Open: a `Content-Security-Policy`. It needs testing in a real browser (Vuetify uses inline styles); a wrong policy would blank the site.

- [x] **14. Backend container ran as root** (`cratewatcher/Dockerfile`)
  - Fixed: runs as the image's `app` user (uid 1654); cache folders created and owned by it. Checked: cover and photo files are written by uid 1654 into the volumes.

- [x] **15. Per-machine IDE files tracked in git**
  - Fixed: `.idea/` (19 files) and `*.DotSettings.user` removed from the git index (files stay on disk) and added to `.gitignore`. The deletions are staged, not committed.

- [~] **16. Dead code and unused packages**
  - Fixed: `CdRelease.CoverArtUrl` property and its write; the `sass` dev dependency and the `allowScripts` block in `package.json`.
  - Put back on purpose: the `name` argument of `createArtist`; the Java backend still requires it (the C# one ignores it).
  - Left: the `cover_art_url` SQL column (needs a new migration to drop); `package-lock.json` still lists `sass` until the next `npm install`.

- [x] **17. The old Java backend is still in the repo** (`cratewatch-server/`)
  - Decided 2026-10-22: **keep it.** Both backends stay on purpose (Java for testing and comparison, C# for production), so the `DatabaseMigrator` Flyway baseline stays too. Nothing deleted.
  - Consequence: the two backends now differ. The gaps are listed under "Java parity gaps" below. The frontend works with both for login, search, adding artists and browsing; the Collection/Wishlist features need the C# one.

- [ ] **18. Thin repository classes** (`*/Repositories/*.cs`, 140 lines)
  - Note: with Java staying, these classes also keep the two backends structurally alike, so leaving them is reasonable.
  - Problem: five classes that mostly wrap one-line `DbContext` calls; one implementation each.
  - Fix: optional: call `db.X` from the services (about -100 lines).

## Not tested yet

- [ ] Concurrent requests (no test of any).
- [ ] `Db/DatabaseMigrator.cs` baseline path and whether a half-failed first migration could be mistaken for a Flyway database.
- [ ] Real deploy: VM, NPMplus, DNS, certificate, Proxmox firewall (steps in `DEPLOY.md`).
- [ ] The new Vue pages and styling in a browser (this audit did not touch them).

## Java parity gaps (informational, from the decision to keep both backends)

What the C# backend has that the Java backend (`cratewatch-server/`) does not:

- [ ] `PUT /releases/{id}/state` and `GET /releases?state=OWNED|WISHLIST` (Collection and Wishlist buttons and pages).
- [ ] `GET /artists/{id}/releases?types=...` with on-demand import of EP, Live, Compilation, Soundtrack, Single, Other (Java imports studio albums only, at add time).
- [ ] `artistId` and `artistName` in the release responses.
- [ ] No open registration: Java still has `POST /auth/register`; C# has the `create-user` command instead.
- [ ] Login rate limit (5 per minute per IP) and the 7-day token (Java: 1 hour, no limit).
- [ ] The artist name comes from the MusicBrainz search, not from the client (Java trusts the client's `name`).
- [ ] Deleted-user tokens rejected, image cache headers, photo URL allow-list, MusicBrainz request gate with retries.

Say which of these you want ported and in what order; none is started.
