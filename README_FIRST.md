# AI Classroom — Full Foundation Build

This ZIP is based on the user's original `AIClassroom` Visual Studio MVC project.

## Included
- ASP.NET Core MVC / .NET 10
- SQL Server LocalDB + Entity Framework Core 10
- Cookie authentication with roles
- Student login: Name + Roll Number + Class + PIN (no email)
- Teacher/SuperAdmin login
- Group A (Class 3–5) and Group B (Class 6–8) curriculum, 25 days each
- Current curriculum state: Group A Days 1–14 published, 15–25 locked; Group B Days 1–8 published, 9–25 locked
- Pixel-art inspired responsive UI with robot/emoji characters
- Student dashboard, lessons, notes, games, quizzes, tests, assignments, scorecard
- Admin dashboard, student management, curriculum lock/unlock, games, quizzes, tests, assignments, teacher notes, announcements, analytics, reports and live class start page
- SignalR ClassroomHub foundation and live classroom room
- XP transactions, levels, badges data model and score settings
- SQL Server database initializer/seed data

## Demo credentials
Teacher:
Username: admin
Password: Admin@123

Student A:
Name: Demo Student A
Roll: 1
Class: 4
PIN: 1234

Student B:
Name: Demo Student B
Roll: 1
Class: 6
PIN: 1234

Change the admin password and demo credentials before school deployment.

## Run
1. Install Visual Studio 2022 with ASP.NET and web development.
2. Install .NET 10 SDK.
3. Install SQL Server Express LocalDB.
4. Open `AIClassroom.csproj`.
5. Let NuGet restore.
6. Press Ctrl+F5.
7. The app creates `AIClassroomDb` automatically in LocalDB on first run.

## Important
This is a large working foundation, not a claim that every advanced item in the long roadmap is production-complete. The database and UI are structured for the remaining expansions: richer game engines, full question-bank authoring, CSV/Excel exports, student live-session joining, class/section administration, notifications, AI teacher assistant and personalized learning.
