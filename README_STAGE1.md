# AI Classroom — Stage 1 (Updated from your project)

This ZIP is based on the original AIClassroom Visual Studio project you provided.

## Included
- ASP.NET Core MVC / .NET 10
- SQL Server LocalDB + Entity Framework Core
- Student, Teacher, Curriculum Module, Curriculum Day and Student Progress entities
- Database initializer
- Group A and Group B curriculum seed
- Group A Days 1–14 published; Days 15–25 locked
- Group B Days 1–8 published; Days 9–25 locked
- SignalR ClassroomHub foundation
- New pixel-art AI Quest landing page
- Existing Bootstrap libraries retained

## Run
1. Extract the ZIP.
2. Open `AIClassroom/AIClassroom.csproj` in Visual Studio 2022.
3. Allow NuGet restore.
4. Press Ctrl+F5.
5. LocalDB database `AIClassroomDb` will be created automatically.

## Demo seed data
Teacher:
Username: admin
Password placeholder: CHANGE-ME

Student A:
Name: Demo Student A
Roll: 1
PIN: 1234
Group: A
Class: 4

Student B:
Name: Demo Student B
Roll: 1
PIN: 1234
Group: B
Class: 6

Authentication is intentionally not implemented yet. It is Stage 2.

## Important
Do not edit generated `bin` or `obj` folders. They are intentionally excluded from this source package.
