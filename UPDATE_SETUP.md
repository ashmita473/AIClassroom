# After replacing the project

From the folder containing `AIClassroom.csproj`:

```powershell
# 1. Restore the local SignalR browser client
libman restore

# 2. Set the production admin password before first production start
$env:AICLASSROOM_ADMIN_PASSWORD = "replace-with-a-strong-password"

# 3. Restore/build
 dotnet restore
 dotnet build

# 4. Run the automated tests from the solution root
 dotnet test
```

The application runs EF Core migrations automatically at startup. Do not use `EnsureCreated` or manually recreate the production database.

For an IIS/reverse-proxy deployment, set `ForwardedHeaders:KnownProxies` to the proxy's IP address(es) and make sure the proxy sends `X-Forwarded-For` and `X-Forwarded-Proto`.
