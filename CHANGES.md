# AIClassroom – hardening & integrity pass

## Security
- Login: per-IP+account lockout (5 fails/5 min), per-account cap (30 fails/15 min), IP rate limit (30/min), constant-time-ish response for unknown accounts, failed logins logged. (`Services/LoginThrottle.cs`, `AccountController`)
- Seeding: admin password now from env `AICLASSROOM_ADMIN_PASSWORD` (min 10 chars) outside Development; demo students/`Admin@123` only in Development.
- Cookies HttpOnly + SameSite=Lax (Secure policy = SameAsRequest; set to Always once on HTTPS); global antiforgery validation.
- Security headers + CSP (nosniff, frame deny, referrer, permissions policy).
- SignalR hub: students can only join their own `Group-Class` room; commands allow-listed; message size limits; arbitrary `object` payload removed. Live room page now access-checked; announcement text no longer injected via innerHTML (XSS).
- Curriculum day pages and lessons: students can only open published, unlocked days of their own group/class.
- Admin AddStudent: no over-posting (XP/IsActive/Id), PIN must be 4–8 digits and not trivial (no default 1234).
- Question image upload: magic-byte allowlist (png/jpg/gif/webp), 2 MB cap, server-chosen extension.

## Integrity (cheating / XP farming)
- Quizzes & tests: start time and question set are stored server-side (session); client can no longer extend the timer or submit only the questions it answered correctly. Late submissions (> limit + 30 s) score as skipped. Refreshing a test no longer restarts its clock.
- XP: games pay once per game (+ perfect bonus once); quizzes pay only for improving best score; tests pay first attempt + first pass; assignments pay once per assignment.
- Lessons can't be completed unless published/unlocked/in the student's class.
- Character shop: an item can only be bought/equipped in its own category.
- Assignment submit: group/published checks, 10k char cap.

## Not done (suggested next)
- Teacher change-password and student PIN-reset UI (currently no way to change a PIN).
- Logout is now POST-only with antiforgery protection.
- Self-host signalr.js (CDN script has no SRI) and move inline scripts to files to drop `'unsafe-inline'` from CSP.
- Switched from `EnsureCreated` to EF migrations (hand-written, no model snapshot yet — see MIGRATIONS.md). NOTE: there is no automated test project in this ZIP.
- Game missions now use server-validated interactive questions; Start Mission no longer posts a fixed score.

## Test System UI & Assessment Tracks
- Added a redesigned student Tests & Assessments experience matching the AI Classroom visual language.
- Added four assessment tracks: Unit Tests, Module Tests, Revision Tests, and Final Assessment.
- Added filterable test cards with question count, time limit, passing score, attempt history and best score.
- Added responsive mobile layout and local `wwwroot/js/test-index.js` filtering (no inline JavaScript).
- Seed data now adds missing Unit, Module, Revision and Final tests for Groups A and B without duplicating existing tests.

## Review pass 2 (this update)
- **Game answers were guessable:** the authored data has the correct option first, so choosing option 1 everywhere scored ~100%. Options are now shuffled per attempt with a server-held seed; submissions without a started attempt are rejected.
- **Migrations:** added `ConfigureWarnings(Ignore(PendingModelChangesWarning))` (hand-written migrations have no ModelSnapshot, which makes EF 9/10 throw at `MigrateAsync`), and made `TeacherProfiles` idempotent for databases that already have the columns.
- **Dev login:** cookies are `Secure=Always` only outside Development; the `http://localhost:5088` profile no longer loops back to login.
- **Lesson HTML:** regex sanitizer replaced by the maintained `HtmlSanitizer` (Ganss.Xss) allow-list (http/https only).
- **Profile picture:** validated by file signature, not file name; last SuperAdmin can no longer demote themselves.
- **SignalR client** bundled in `wwwroot/lib/microsoft-signalr` (was an empty folder, so the live class would not connect without `libman restore`).
- CSP `img-src` now allows `https:` so the lesson editor's "insert image URL" works; remove it if you only want local images.
