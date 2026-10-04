# AI Classroom build status

## Implemented in this build
- ASP.NET Core MVC / .NET 10
- SQL Server LocalDB / EF Core 10
- Cookie authentication + roles
- Student login: name + roll + class + PIN
- Teacher/SuperAdmin login
- Group A and B 25-day curriculum seeded from the supplied curriculum
- Group A Days 1–14 published and Days 15–25 locked
- Group B Days 1–8 published and Days 9–25 locked
- Student dashboard
- Curriculum world / day pages
- Lesson completion + XP transactions
- Notes
- Games catalogue + score/XP recording
- Quizzes + automatic scoring
- Tests + automatic scoring
- Assignments + student submission
- Scorecard
- Class leaderboard
- Badge data + automatic awards for several milestones
- Teacher dashboard
- Student management/search/filter/activate/deactivate
- Curriculum lock/unlock
- Game/quiz/test/assignment creation shells
- Teacher notes
- Announcements
- Analytics
- Reports summary
- Live class creation and SignalR classroom hub
- Student live-class discovery/join
- Pixel-art-inspired responsive UI and robot characters

## Not production-complete yet
- Full question-bank CRUD UI for quizzes/tests
- Full game engines for all 20 named games (current game cards record demo completion; individual interactive games are next)
- CSV/Excel export
- Student/team live answer collection and live scoring
- Dynamic school groups/sections CRUD
- Teacher profile/change-password UI
- Database backup/restore UI
- Notifications inbox
- AI teacher assistant / AI tutor / personalization
- Local bundled SignalR browser client; current live room references the browser client CDN

The last items are intentionally separated so the core application remains understandable and maintainable.


## Pixel Login UI update
- Added generated pixel-art AI Classroom background at wwwroot/images/ai-classroom-login-bg.png.
- Replaced student Login.cshtml with functional pixel-art login matching the supplied visual reference.
- Added Press Start 2P + VT323 web fonts and responsive login styling in wwwroot/css/login-pixel.css.
