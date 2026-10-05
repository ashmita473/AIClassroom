# AI Classroom deployment/security checklist

- Set `AICLASSROOM_ADMIN_PASSWORD` in the server environment before first production start. Use at least 10 characters. The development-only demo password is never used in production.
- The authentication, antiforgery and session cookies now use `CookieSecurePolicy.Always`; serve the application over HTTPS.
- EF Core starts with `Database.MigrateAsync()` instead of `EnsureCreatedAsync()`.
- If IIS, nginx, Apache or another reverse proxy sits in front of the app, put the proxy IP addresses in `ForwardedHeaders:KnownProxies` in configuration. Forward `X-Forwarded-For` and `X-Forwarded-Proto`. Forwarded headers are processed before the rate limiter so login throttling can use the original client IP.
- SignalR is loaded from `wwwroot/lib/microsoft-signalr`. If the local client file is not present in the ZIP, run `libman restore` (or install `@microsoft/signalr@10.0.0` and copy `dist/browser/signalr.min.js` into that folder) before deployment.
- The CSP no longer permits inline scripts or external script CDNs. Inline application scripts were moved into `wwwroot/js`.
- Logout is POST-only and protected by antiforgery.
- Student PINs and teacher passwords are changed/reset through authenticated screens and are stored as PBKDF2 hashes.
- Game submissions no longer accept a client-provided score. The server rebuilds the game challenge and calculates the score from submitted answers.
