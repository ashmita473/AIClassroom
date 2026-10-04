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
- Teacher "Logout" is still a GET link (low risk); move to POST form.
- Self-host signalr.js (CDN script has no SRI) and move inline scripts to files to drop `'unsafe-inline'` from CSP.
- Switch from `EnsureCreated` to EF migrations; add automated tests (quiz scoring, authorization).
- Real game engines (Start Mission still posts a fixed score of 100 — XP is now capped so it's no longer exploitable).
