# Quiz + Test System — Stage Update

Implemented in the existing AI Classroom ASP.NET Core MVC project.

## Student quiz system
- MCQ
- True/False
- Image question
- Match the following
- Ordering
- Scenario question
- Short answer
- Countdown timer with automatic submit
- Randomized question order
- Maximum attempts
- Passing score
- Automatic scoring
- Instant result page when enabled by teacher
- Question bank with topic and difficulty
- Per-question points
- Topic performance
- Strong areas / needs revision
- Quiz XP: +10 XP per correct answer

## Student test system
- Unit tests
- Module tests
- Revision tests
- Final assessment
- Countdown timer
- Randomized question order
- Automatic scoring
- Score / percentage
- Correct / wrong / skipped
- Time taken
- Topic performance
- Strong areas / needs revision
- Test XP: +100 XP when completed and +25 XP when not passed

## Teacher/admin
- Create quizzes
- Create tests
- Configure time, passing score, attempts and publishing
- Add all supported question types
- Upload image questions
- Add question topic, difficulty, points and explanation
- Reusable question bank
- Copy a banked question into another quiz/test
- Delete questions
- View quiz results
- View test results

## Student scorecard
- Total XP
- Lessons completed
- Games played
- Quizzes
- Tests
- Overall assessment accuracy
- Topic performance bars
- Strong areas
- Needs revision
- XP history

## Database note
This project now uses `Database.MigrateAsync()`. The initial migration creates the schema for a new database and safely skips creation for tables that already exist.

From the folder containing `AIClassroom.csproj`:

```powershell
dotnet ef database drop --force --project .
dotnet run
```

If `dotnet ef` is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

Then run the database-drop command again.
