# Open issues

Found in the review of the C# port. Fix one at a time, tick the box when done.
Code references use file + method names because line numbers move.

Order: 1–5 before other people use the app, 6 is a decision, 7–10 are optional.

## Should fix

- [x] **1. Race when adding an artist** — fixed: `ArtistService.CreateArtistAsync` now runs under a striped in-process lock per MBID, and a failed import of any kind deletes the half-imported artist. Verified with 4 parallel `POST /artists` (before: 3× 500 duplicate key; after: 201+409 per user, 1 artist, 10 releases). Limit: in-process lock only, see the `shortcut:` comment.
  - Where: `Artists/Services/ArtistService.cs` → `CreateArtistAsync`, `ImportNewArtistAsync`
  - Problem: check-then-write. Two clicks, or two users adding the same new artist at once, both pass the "exists?" check. The second insert breaks UNIQUE on `artists.musicbrainz_id` or `user_artists(user_id, artist_id)` and returns 500. User B can also see user A's half-imported artist (import takes seconds), skip the import, and get an empty release list that is never re-imported.
  - Fix: catch the unique-violation `DbUpdateException`. On `user_artists` return the 409 "already exists" path. On `artists`, re-read the artist by MBID.
  - Test: two parallel `POST /artists` with the same MBID. Expect 201 + 409, never 500.

- [x] **2. Client-supplied artist name and MBID are trusted (shared catalog)** — fixed: `/artists/search` caches each result (1h, in-memory); `POST /artists` takes only `musicbrainzId`, uses the cached MusicBrainz name, and returns 400 "search first" if the id was not in a recent search. Ids are lowercased, max 36 chars, names capped at 255. Verified live. Limit: cache is lost on restart (see `shortcut:` comment).
  - Where: `Artists/Domain/ArtistDtos.cs` → `CreateArtistRequest`, `ArtistService.ImportNewArtistAsync`
  - Problem: user A posts a real MBID with a fake name. Everyone who adds that MBID later gets the fake name, with no way to fix it. The MBID is not validated. `musicbrainzId` over 36 chars or `name` over 255 chars gives a 500 (DB column limits).
  - Fix: validate the MBID as a GUID, add `[StringLength]` on both fields. Better: take the name from MusicBrainz instead of the request.
  - Test: bad MBID → 400; 300-char name → 400.

- [x] **3. Username has no length limit, register race** — fixed: `[StringLength(100)]` on username; `Register` now relies on the unique index (catches the Postgres unique violation → 409) and the racy pre-check plus `ExistsByUsernameAsync` were deleted. Verified live: 101 chars → 400, 4 parallel same-name registers → one 201 + three 409, one DB row.
  - Where: `Users/Domain/AuthDtos.cs` → `RegisterRequest`, `Users/Controllers/AuthController.cs` → `Register`
  - Problem: DB column is `VARCHAR(100)`, request only checks "not blank". 101-char username → 500. Two simultaneous registrations of one name → 500 (unique index fires after the check passed).
  - Fix: `[StringLength(100)]` on username. Catch the unique violation and return 409.
  - Test: 101-char username → 400; two parallel registers → 201 + 409.

- [x] **4. A temporary TheAudioDB error hides the photo forever** — fixed: `ArtistPhotoCacheService` writes the `.missing` marker only for a clean "no thumb" answer or an image 404; lookup errors, bad JSON, and other image HTTP errors return null and retry next request. 5 tests in `ArtistPhotoCacheServiceTests`.
  - Where: `TheAudioDb/ArtistPhotoCacheService.cs` → `LookupThumbUrlAsync`, `LookupAndCacheAsync`
  - Problem: when the lookup throws, `null` is returned. The caller reads that as "no photo" and writes the permanent `.missing` file. One timeout or rate-limit and that artist never gets a photo until the file is deleted by hand.
  - Fix: do not write the marker when the lookup failed. Only mark missing after a clean "no thumb" answer.
  - Test: fake `TheAudioDbClient` that throws → no `.missing` file; next request tries again.

- [x] **5. Default JWT secret is public** — fixed: `Program.cs` now refuses to start outside Development without `JWT_GENERATOR_SIGNATURE_SECRET` (and rejects secrets under 32 bytes). `docker-compose.yml` requires it, `.env.example` lists it, `Properties/launchSettings.json` sets Development for `dotnet run`.
  - Where: `Program.cs` (`JWT_GENERATOR_SIGNATURE_SECRET` fallback), `docker-compose.yml`, `.env.example`
  - Problem: if the env var is missing, the signing key is the string in this repo. Anyone who can read the repo can forge a token for any username.
  - Fix: throw at startup when the var is missing and the environment is not Development. Add the var to `.env.example` and the compose file.
  - Test: start with `ASPNETCORE_ENVIRONMENT=Production` and no secret → app refuses to start.

## Decision needed

- [x] **6. Import keeps only `STUDIO_ALBUM`, docs say all types** — decided: keep studio-only. `CONTEXT.md` updated (glossary + decisions log), dead `COVER_TYPE_PRIORITY` removed in Java and C#, cover pick simplified, classify-before-exists-check. `ReleaseTypeClassifier` left as is (still labels old rows); frontend EP/Compilation sections left as is.
  - Where: Java `cratewatch-server/.../ArtistImportService.java` (`COVER_TYPE_PRIORITY`, the `Objects.equals` filter); C# `Artists/Services/ArtistImportService.cs` (same logic); `CONTEXT.md`
  - Problem: only studio albums are saved, but `CONTEXT.md` says all types are imported and filtered in the UI. The other six entries in the cover-priority list and most of `ReleaseTypeClassifier` can never matter. An artist with no studio albums gets no cover release. Artists imported before the filter keep their other types.
  - Fix: pick one. Studio-only: update `CONTEXT.md`, delete the dead priority list. All types: remove the filter in both Java and C#.
  - Do the same in both backends so they stay equal.

## Nice to have

- [x] **7. Nested `.git` inside `cratewatcher/`** — fixed: nested repo removed (backup of its 1 commit kept outside the repo), parent repo now sees all files; bin/obj/cache stay ignored. Nothing committed yet.
  - Problem: the IDE created a repo with no commits inside the folder. `git add` in the parent records an empty gitlink, not the files.
  - Fix: delete `cratewatch-server-c/cratewatcher/.git`, or make it a real submodule on purpose.

- [x] **8. The two cache services are near copies** — fixed: new `Caching/ImageFileCache` owns the `{id}.jpg` / `{id}.missing` logic; `CoverArtCacheService` and `ArtistPhotoCacheService` only pass a fetch function. Issue 10 is now a one-place change. 4 cover tests added.
  - Where: `CoverArt/CoverArtCacheService.cs`, `TheAudioDb/ArtistPhotoCacheService.cs`
  - Problem: both do "read file → check `.missing` marker → fetch → write marker". Issue 4 and issue 10 must be fixed in two places.
  - Fix: one small shared helper. Do it together with 4 and 10, or skip.

- [x] **9. The `user is null → 401` block repeats 4 times** — fixed: JWT `OnTokenValidated` (Program.cs) looks the user up once, rejects unknown users with 401 and adds a `uid` claim; `ArtistsController` reads `CurrentUserId` from it and no longer needs `UserRepository`; `ArtistService.CreateArtistAsync` takes a `Guid userId`. Ownership and deleted-user cases re-run live.
  - Where: `Artists/Controllers/ArtistsController.cs`
  - Fix: one helper, or put the user id in the JWT and drop the per-request user lookup.

- [x] **10. Cache files are written in one non-atomic step** — fixed in one place: `ImageFileCache.WriteAtomicAsync` writes a temp file then `File.Move(overwrite)`; temp file deleted on failure. 4 tests in `ImageFileCacheTests`.
  - Where: `ArtistPhotoCacheService.cs`, `CoverArtCacheService.cs` (`File.WriteAllBytesAsync`)
  - Problem: a second request for the same item can read a half-written file and serve a broken JPEG.
  - Fix: write to a temp name, then `File.Move` into place.

## Not tested yet

- [x] Ownership check — run live (manual, no automated test): non-owner gets 404 on `/artists/{id}` and `/{id}/releases` and an empty list; anonymous gets 401; random or non-GUID id gets 404; after adding the artist the user gets 200; a token for a deleted user gets 401.
- [ ] Concurrent requests (no test of any).
- [ ] `Db/DatabaseMigrator.cs` baseline path: only tried by hand. A first migration that fails halfway (Postgres older than 18, no `uuidv7()`) might later be mistaken for a Flyway-created schema. Consider an integration test, or `WithTransactionPerScript()`.
- [ ] Vue frontend against the C# server.

## Done

- [x] Sonar S6966: `app.Run()` → `await app.RunAsync()` (`Program.cs`)
- [x] Sonar S2139: removed log-and-rethrow in `ArtistService.SearchAlbumsAsync` and `ImportNewArtistAsync`
