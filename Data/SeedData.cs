using AIClassroom.Models;
using AIClassroom.Services;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db, PasswordService passwords, bool isDevelopment = false, ILogger? logger = null)
    {
        // Admin password comes from AICLASSROOM_ADMIN_PASSWORD. The well-known demo password is used in Development only.
        if (!await db.Teachers.AnyAsync())
        {
            var adminPassword = Environment.GetEnvironmentVariable("AICLASSROOM_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(adminPassword) && isDevelopment) adminPassword = "Admin@123";
            if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 10 && !isDevelopment)
                logger?.LogWarning("No admin account created: set AICLASSROOM_ADMIN_PASSWORD (min 10 chars) and restart.");
            else
                db.Teachers.Add(new Teacher { Name = "AI Classroom Admin", Username = "admin", PasswordHash = passwords.Hash(adminPassword), Role = "SuperAdmin" });
        }

        // Demo students (PIN 1234) are Development-only.
        if (isDevelopment && !await db.Students.AnyAsync())
        {
            db.Students.Add(new Student { Name = "Demo Student A", RollNumber = "1", PinHash = passwords.Hash("1234"), GroupName = "A", ClassNumber = 4, Avatar = "ROBO", XpBalance = 200 });
            db.Students.Add(new Student { Name = "Demo Student B", RollNumber = "1", PinHash = passwords.Hash("1234"), GroupName = "B", ClassNumber = 6, Avatar = "FOX", XpBalance = 200 });
        }

        // Seed/update score settings without creating duplicate keys.
        // Do not combine AddRange() with AnyAsync() here before SaveChangesAsync(),
        // because newly added entities are not yet in SQL Server and can be added twice.
        var requiredScoreSettings = new Dictionary<string, int>
        {
            ["LessonCompleted"] = 50,
            ["QuizCorrect"] = 10,
            ["GameCompleted"] = 50,
            ["PerfectGame"] = 100,
            ["TestCompleted"] = 100,
            ["AssignmentCompleted"] = 50,
            ["Participation"] = 10
        };

        var existingScoreSettings = await db.ScoreSettings
            .ToDictionaryAsync(x => x.Key);

        foreach (var setting in requiredScoreSettings)
        {
            if (existingScoreSettings.TryGetValue(setting.Key, out var existing))
            {
                existing.Points = setting.Value;
            }
            else
            {
                db.ScoreSettings.Add(new ScoreSetting
                {
                    Key = setting.Key,
                    Points = setting.Value
                });
            }
        }

        if (!await db.Badges.AnyAsync())
        {
            db.Badges.AddRange(
                new Badge { Name = "First Lesson", Description = "Complete your first lesson.", Icon = "📚" },
                new Badge { Name = "First Game", Description = "Complete your first game.", Icon = "🎮" },
                new Badge { Name = "Quiz Master", Description = "Score 100% on a quiz.", Icon = "🧪" },
                new Badge { Name = "Quiz Finisher", Description = "Pass a quiz.", Icon = "🎯" },
                new Badge { Name = "Pattern Detective", Description = "Master a pattern mission.", Icon = "🔍" },
                new Badge { Name = "Robot Trainer", Description = "Complete a robot training game.", Icon = "🤖" },
                new Badge { Name = "AI Explorer", Description = "Complete 10 learning activities.", Icon = "🚀" },
                new Badge { Name = "Perfect Score", Description = "Achieve a perfect test score.", Icon = "🏆" },
                new Badge { Name = "7-Day Streak", Description = "Learn for seven days in a row.", Icon = "🔥" },
                new Badge { Name = "Algorithm Master", Description = "Complete the algorithm mission.", Icon = "🧩" },
                new Badge { Name = "AI Creator", Description = "Complete a creative AI mission.", Icon = "🎨" },
                new Badge { Name = "Young AI Scientist", Description = "Complete a full curriculum path.", Icon = "🧠" });
        }

        await db.SaveChangesAsync();

        if (!await db.CurriculumModules.AnyAsync())
        {
            AddGroupA(db);
            AddGroupB(db);
            await db.SaveChangesAsync();
            await SeedLessonContent(db);
        }

        if (!await db.Games.AnyAsync())
        {
            db.Games.AddRange(
                new Game { Name = "AI or Not AI", GroupName = "A", GameType = "classification", Description = "Decide whether everyday examples use AI.", BaseXp = 50 },
                new Game { Name = "Pattern Detective", GroupName = "A", GameType = "pattern", Description = "Find the hidden pattern.", BaseXp = 50 },
                new Game { Name = "Robot Trainer", GroupName = "A", GameType = "sequence", Description = "Give Robo the correct instructions.", BaseXp = 75 },
                new Game { Name = "Sensor Hunt", GroupName = "A", GameType = "matching", Description = "Match sensors to what machines detect.", BaseXp = 50 },
                new Game { Name = "Classification Game", GroupName = "A", GameType = "classification", Description = "Sort objects into groups.", BaseXp = 75 },
                new Game { Name = "Chatbot Challenge", GroupName = "A", GameType = "logic", Description = "Choose the best chatbot response.", BaseXp = 50 },
                new Game { Name = "Algorithm Maze", GroupName = "A", GameType = "maze", Description = "Guide Robo through ordered instructions.", BaseXp = 100 },
                new Game { Name = "Flowchart Builder", GroupName = "A", GameType = "flowchart", Description = "Build a yes/no decision flow.", BaseXp = 100 },
                new Game { Name = "AI Creator", GroupName = "A", GameType = "creative", Description = "Combine prompts to create a scene.", BaseXp = 75 },
                new Game { Name = "Recommendation Detective", GroupName = "A", GameType = "data", Description = "Discover why apps recommend things.", BaseXp = 75 },
                new Game { Name = "ML Trainer", GroupName = "B", GameType = "training", Description = "Train a tiny classifier with examples.", BaseXp = 100 },
                new Game { Name = "Dataset Detective", GroupName = "B", GameType = "data", Description = "Find useful and messy data.", BaseXp = 75 },
                new Game { Name = "Classification Lab", GroupName = "B", GameType = "classification", Description = "Classify new examples.", BaseXp = 100 },
                new Game { Name = "Accuracy Challenge", GroupName = "B", GameType = "accuracy", Description = "Calculate model accuracy.", BaseXp = 100 },
                new Game { Name = "Bias Detective", GroupName = "B", GameType = "ethics", Description = "Spot unfair training data.", BaseXp = 100 },
                new Game { Name = "NLP Challenge", GroupName = "B", GameType = "nlp", Description = "Explore language processing.", BaseXp = 100 },
                new Game { Name = "Sentiment Challenge", GroupName = "B", GameType = "sentiment", Description = "Classify text sentiment.", BaseXp = 75 },
                new Game { Name = "Prompt Battle", GroupName = "B", GameType = "prompt", Description = "Improve prompts for better output.", BaseXp = 100 },
                new Game { Name = "AI Model Builder", GroupName = "B", GameType = "model", Description = "Connect input, model and output.", BaseXp = 100 },
                new Game { Name = "AI Problem Solver", GroupName = "B", GameType = "problem", Description = "Design an AI solution.", BaseXp = 125 });
            await db.SaveChangesAsync();
        }

        if (!await db.Quizzes.AnyAsync())
        {
            var quizA = new Quiz { Title = "AI Basics Quick Quiz", GroupName = "A", TimeLimitMinutes = 10, PassingScore = 60, MaxAttempts = 3, IsPublished = true };
            quizA.Questions.Add(Q("What does AI try to do?", new[] { ("Act intelligently", true), ("Only calculate", false), ("Only store files", false), ("Only play music", false) }));
            quizA.Questions.Add(Q("Which can act as an AI sensor?", new[] { ("Camera", true), ("Chair", false), ("Book", false), ("Pencil", false) }));
            quizA.Questions.Add(Q("AI can learn from examples called...", new[] { ("Data", true), ("Paint", false), ("Paper", false), ("Buttons", false) }));
            var quizB = new Quiz { Title = "AI + ML Foundations", GroupName = "B", TimeLimitMinutes = 10, PassingScore = 60, MaxAttempts = 3, IsPublished = true };
            quizB.Questions.Add(Q("ML mainly learns from...", new[] { ("Data", true), ("Electricity", false), ("Screens", false), ("Keyboard shortcuts", false) }));
            quizB.Questions.Add(Q("Which learning type uses correct labels?", new[] { ("Supervised", true), ("Unsupervised", false), ("Reinforcement", false), ("Manual", false) }));
            quizB.Questions.Add(Q("A pixel is a small part of an...", new[] { ("Image", true), ("Algorithm", false), ("Audio cable", false), ("Database table", false) }));
            db.Quizzes.AddRange(quizA, quizB);
            await db.SaveChangesAsync();
        }

        if (!await db.Tests.AnyAsync())
        {
            var testA = new Test { Title = "Group A AI Revision Test", TestType = "Revision", GroupName = "A", TimeLimitMinutes = 15, PassingScore = 60, IsPublished = true };
            testA.Questions.Add(Q("Which is an example of AI in daily life?", new[] { ("Face unlock", true), ("A plain chair", false), ("A pencil", false), ("A wall", false) }));
            testA.Questions.Add(Q("What is an algorithm?", new[] { ("Step-by-step instructions", true), ("A color", false), ("A sensor", false), ("A battery", false) }));
            var testB = new Test { Title = "Group B AI + ML Revision Test", TestType = "Revision", GroupName = "B", TimeLimitMinutes = 15, PassingScore = 60, IsPublished = true };
            testB.Questions.Add(Q("Which ML type learns with labeled examples?", new[] { ("Supervised", true), ("Unsupervised", false), ("Reinforcement", false), ("Random", false) }));
            testB.Questions.Add(Q("Accuracy compares correct predictions with...", new[] { ("Total predictions", true), ("Screen size", false), ("Pixel color", false), ("Keyboard speed", false) }));
            db.Tests.AddRange(testA, testB);
            await db.SaveChangesAsync();
        }


        if (!await db.CharacterItems.AnyAsync())
        {
            db.CharacterItems.AddRange(
                // Skin tones
                new CharacterItem { Category="Skin", Name="Warm", StyleKey="skin-warm", Description="A warm starter skin tone.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Skin", Name="Peach", StyleKey="skin-peach", Description="A soft peach tone.", Cost=25 },
                new CharacterItem { Category="Skin", Name="Honey", StyleKey="skin-honey", Description="A warm honey tone.", Cost=50 },

                // Hair
                new CharacterItem { Category="Hair", Name="Starter Hair", StyleKey="hair-short", Description="Simple starter hair.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Hair", Name="Spiky", StyleKey="hair-spiky", Description="A playful spiky style.", Cost=75 },
                new CharacterItem { Category="Hair", Name="Bob", StyleKey="hair-bob", Description="A cute bob cut.", Cost=75 },
                new CharacterItem { Category="Hair", Name="Long Wave", StyleKey="hair-long", Description="Long pixel waves.", Cost=150 },
                new CharacterItem { Category="Hair", Name="Galaxy Hair", StyleKey="hair-galaxy", Description="A rare cosmic style.", Cost=500 },

                // Hair colors
                new CharacterItem { Category="HairColor", Name="Midnight", StyleKey="hair-black", Description="Classic dark hair.", Cost=0, IsDefault=true },
                new CharacterItem { Category="HairColor", Name="Brown", StyleKey="hair-brown", Description="Warm brown hair.", Cost=25 },
                new CharacterItem { Category="HairColor", Name="Pink", StyleKey="hair-pink", Description="Bright pixel pink.", Cost=100 },
                new CharacterItem { Category="HairColor", Name="Blue", StyleKey="hair-blue", Description="Electric blue.", Cost=150 },
                new CharacterItem { Category="HairColor", Name="Rainbow", StyleKey="hair-rainbow", Description="A colorful rare style.", Cost=400 },

                // Outfits
                new CharacterItem { Category="Outfit", Name="Academy Uniform", StyleKey="outfit-uniform", Description="Starter AI Academy uniform.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Outfit", Name="Explorer", StyleKey="outfit-explorer", Description="Ready for missions.", Cost=100 },
                new CharacterItem { Category="Outfit", Name="Space Suit", StyleKey="outfit-space", Description="A cosmic explorer suit.", Cost=250 },
                new CharacterItem { Category="Outfit", Name="Dragon Hoodie", StyleKey="outfit-dragon", Description="A rare dragon hoodie.", Cost=500 },

                // Hats
                new CharacterItem { Category="Hat", Name="None", StyleKey="hat-none", Description="No hat.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Hat", Name="Study Cap", StyleKey="hat-study", Description="A tiny learning cap.", Cost=75 },
                new CharacterItem { Category="Hat", Name="Wizard Hat", StyleKey="hat-wizard", Description="For curious young wizards.", Cost=200 },
                new CharacterItem { Category="Hat", Name="Astronaut Helmet", StyleKey="hat-space", Description="Mission-ready helmet.", Cost=350 },
                new CharacterItem { Category="Hat", Name="Crown", StyleKey="hat-crown", Description="A legendary achievement crown.", Cost=1000 },

                // Accessories
                new CharacterItem { Category="Accessory", Name="None", StyleKey="acc-none", Description="No accessory.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Accessory", Name="Smart Glasses", StyleKey="acc-glasses", Description="Pixel smart glasses.", Cost=100 },
                new CharacterItem { Category="Accessory", Name="Headphones", StyleKey="acc-headphones", Description="Listen to your mission audio.", Cost=150 },
                new CharacterItem { Category="Accessory", Name="Jetpack", StyleKey="acc-jetpack", Description="A legendary-looking jetpack.", Cost=750 },

                // Shoes
                new CharacterItem { Category="Shoes", Name="Starter Shoes", StyleKey="shoes-basic", Description="Starter sneakers.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Shoes", Name="Speed Shoes", StyleKey="shoes-speed", Description="Zoom through missions.", Cost=150 },
                new CharacterItem { Category="Shoes", Name="Moon Boots", StyleKey="shoes-moon", Description="Rare moon boots.", Cost=300 },

                // Pets
                new CharacterItem { Category="Pet", Name="No Pet", StyleKey="pet-none", Description="Explore solo.", Cost=0, IsDefault=true },
                new CharacterItem { Category="Pet", Name="Pixel Chick", StyleKey="pet-chick", Description="A tiny starter companion.", Cost=200 },
                new CharacterItem { Category="Pet", Name="Cat", StyleKey="pet-cat", Description="A loyal pixel cat companion.", Cost=1000 },
                new CharacterItem { Category="Pet", Name="Space Fox", StyleKey="pet-fox", Description="A rare cosmic fox.", Cost=1500 },
                new CharacterItem { Category="Pet", Name="Dragon", StyleKey="pet-dragon", Description="Legendary companion.", Cost=2500 }
            );
            await db.SaveChangesAsync();
        }

        // Give every seeded student the default character and starter items.
        var studentsForCharacters = await db.Students.ToListAsync();
        var itemsForCharacters = await db.CharacterItems.ToListAsync();
        foreach (var st in studentsForCharacters)
        {
            var character = await db.StudentCharacters.FirstOrDefaultAsync(x => x.StudentId == st.Id);
            if (character == null)
            {
                var skin = itemsForCharacters.First(x => x.StyleKey == "skin-warm");
                var hair = itemsForCharacters.First(x => x.StyleKey == "hair-short");
                var hairColor = itemsForCharacters.First(x => x.StyleKey == "hair-black");
                var outfit = itemsForCharacters.First(x => x.StyleKey == "outfit-uniform");
                var hat = itemsForCharacters.First(x => x.StyleKey == "hat-none");
                var acc = itemsForCharacters.First(x => x.StyleKey == "acc-none");
                var shoes = itemsForCharacters.First(x => x.StyleKey == "shoes-basic");
                var pet = itemsForCharacters.First(x => x.StyleKey == "pet-none");
                db.StudentCharacters.Add(new StudentCharacter {
                    StudentId=st.Id, BodyType="Boy",
                    SkinToneItemId=skin.Id, HairItemId=hair.Id, HairColorItemId=hairColor.Id,
                    OutfitItemId=outfit.Id, HatItemId=hat.Id, AccessoryItemId=acc.Id,
                    ShoesItemId=shoes.Id, PetItemId=pet.Id
                });
            }

            foreach (var item in itemsForCharacters.Where(x => x.IsDefault))
            {
                if (!await db.StudentCharacterItems.AnyAsync(x => x.StudentId == st.Id && x.CharacterItemId == item.Id))
                    db.StudentCharacterItems.Add(new StudentCharacterItem { StudentId=st.Id, CharacterItemId=item.Id });
            }
        }
        await db.SaveChangesAsync();

        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.Add(new Announcement { Title = "Welcome to AI Quest!", Message = "Your AI learning adventure starts here. Complete today's mission and earn XP!" });
            await db.SaveChangesAsync();
        }
    }

    static Question Q(string prompt, (string text, bool correct)[] options)
    {
        var q = new Question { Prompt = prompt, QuestionType = "MCQ" };
        foreach (var (text, correct) in options) q.Options.Add(new QuestionOption { Text = text, IsCorrect = correct });
        return q;
    }

    static void AddGroupA(AppDbContext db)
    {
        var modules = new (string code, string title, int order, (int day, string title, string summary)[] days)[]
        {
            ("A1", "What is AI", 1, new[] { (1,"Intelligence & AI Introduction","AI in phones, apps and homes. AI is already around you."),(2,"AI in Daily Life","Intelligence, humans vs machines and smart machines."),(3,"AI vs Non-AI","Learning/adapting vs fixed rules; use the question: Does it learn?") }),
            ("A2", "Sensors", 2, new[] { (4,"Human vs AI Senses","Human eyes, ears and touch compared with machine sensors."),(5,"Types of Sensors","Camera, microphone and touch sensors."),(6,"How AI Uses Sensors","Face recognition, voice recognition and input → processing → output.") }),
            ("A3", "Patterns", 3, new[] { (7,"What is a Pattern","Repeating things, sequences and patterns in daily life."),(8,"Sorting & Classification","Group by color, size or shape; multiple grouping ways."),(9,"How AI Learns from Patterns","Humans and AI learn from examples; cat vs dog and spam vs important.") }),
            ("A4", "Language & Chatbots", 4, new[] { (10,"Talking to Machines","Voice commands, wake words and input → command → response."),(11,"Chatbots","What chatbots are, where we see them and pattern-based responses."),(12,"Conversations","Question → answer flow and simple if/then logic; limitations.") }),
            ("A5", "AI in World + Ethics", 5, new[] { (13,"AI Applications","Healthcare, farming, weather forecasting and space."),(14,"Ethics & Concerns","Can AI be wrong? Good/bad AI, trust and safety.") }),
            ("A6", "Algorithms", 6, new[] { (15,"What is an Algorithm","Step-by-step instructions with daily examples."),(16,"Instructions & Logic","Order matters and debugging mistakes."),(17,"Flowcharts","Start → steps → decision and yes/no thinking.") }),
            ("A7", "Creative AI", 7, new[] { (18,"What is Creative AI","AI art, music and stories."),(19,"Working with AI","Human input → AI generated output."),(20,"Creativity vs Copying","Does AI create or copy? Discussion.") }),
            ("A8", "Recommendation", 8, new[] { (21,"Recommendation Systems","YouTube/Netflix suggestions based on history."),(22,"Data & Privacy","What data is collected and why suggestions change.") }),
            ("A9-A10", "Problem Solving + Presentation", 9, new[] { (23,"Problem Identification","School/home problems and where AI can help."),(24,"AI Solution Design","Idea → how it works → benefit."),(25,"Presentation + Revision","Explain the idea and recap all modules.") })
        };
        foreach (var m in modules)
        {
            var module = new CurriculumModule { GroupName = "A", ClassMin = 3, ClassMax = 5, Code = m.code, Title = m.title, SortOrder = m.order };
            foreach (var d in m.days) module.Days.Add(new CurriculumDay { DayNumber = d.day, Title = d.title, ContentSummary = d.summary, IsPublished = d.day <= 14, IsLocked = d.day > 14, XpReward = 50 });
            db.CurriculumModules.Add(module);
        }
    }

    static void AddGroupB(AppDbContext db)
    {
        var modules = new (string code, string title, int order, (int day, string title, string summary)[] days)[]
        {
            ("B1", "AI + ML", 1, new[] { (1,"AI Basics","AI vs ML vs DL and rule-based vs learning-based systems."),(2,"ML Types","Supervised, unsupervised and reinforcement learning."),(3,"AI Evolution + ChatGPT","AI history and the prediction idea behind ChatGPT.") }),
            ("B2", "Data", 2, new[] { (4,"Types of Data","Text, image, audio and tabular data."),(5,"Data Processing","Cleaning, organizing and basic mean/median ideas."),(6,"Bias","What bias is and why it can be dangerous.") }),
            ("B3", "Computer Vision", 3, new[] { (7,"Images as Data","Pixels and image representation."),(8,"Classification","How models recognize objects."),(9,"Accuracy","Correct vs incorrect predictions and errors.") }),
            ("B4", "NLP", 4, new[] { (10,"Language Processing","Tokenization idea."),(11,"Sentiment Analysis","Positive/negative classification."),(12,"Chatbots","How chatbots work and limitations.") }),
            ("B5", "Ethics", 5, new[] { (13,"Ethics Concepts","Bias, privacy and fairness."),(14,"Real Issues","Deepfakes, AI misuse and jobs debate.") }),
            ("B6", "No-Code AI", 6, new[] { (15,"Prototypes","What an AI model is."),(16,"Input → Model → Output","Understanding the basic pipeline."),(17,"Testing & Improving","Test a model and improve it.") }),
            ("B7", "Generative AI", 7, new[] { (18,"How AI Generates","Prediction concept."),(19,"Prompting","Better input → better output."),(20,"Limitations","Hallucination and wrong answers.") }),
            ("B8", "AI in India + Problem Solving", 8, new[] { (21,"AI in India","Examples and discussion."),(22,"Problem Solving","Use AI thinking to solve problems.") }),
            ("B9-B10", "Project", 9, new[] { (23,"Project Planning","Plan the AI project."),(24,"Build","Build the project."),(25,"Presentation","Present the project.") })
        };
        foreach (var m in modules)
        {
            var module = new CurriculumModule { GroupName = "B", ClassMin = 6, ClassMax = 8, Code = m.code, Title = m.title, SortOrder = m.order };
            foreach (var d in m.days) module.Days.Add(new CurriculumDay { DayNumber = d.day, Title = d.title, ContentSummary = d.summary, IsPublished = d.day <= 8, IsLocked = d.day > 8, XpReward = 50 });
            db.CurriculumModules.Add(module);
        }
    }

    static async Task SeedLessonContent(AppDbContext db)
    {
        var days = await db.CurriculumDays.ToListAsync();
        foreach (var d in days)
        {
            var lesson = new Lesson
            {
                CurriculumDayId = d.Id,
                Introduction = $"Welcome to Day {d.DayNumber}: {d.Title}.",
                Explanation = d.ContentSummary,
                Examples = "Explore the examples with Robo, answer the mission questions and discuss your ideas with the teacher.",
                TeacherNotes = "Use the lesson as a guided classroom activity. Ask students to explain their answer in their own words.",
                StudentNotesTemplate = $"Day {d.DayNumber}: {d.Title}\n\nMy important points:\n- \n- \n- ",
                IsPublished = d.IsPublished
            };
            lesson.Activities.Add(new Activity { ActivityType = "Warmup", Title = "🤖 Robo Warm-up", Content = "Think about one real-life example connected to today's topic.", SortOrder = 1, XpReward = 10 });
            lesson.Activities.Add(new Activity { ActivityType = "Challenge", Title = "🎮 Mission Challenge", Content = "Complete the classroom challenge and explain why your answer makes sense.", SortOrder = 2, XpReward = 20 });
            lesson.Activities.Add(new Activity { ActivityType = "Reflection", Title = "🧠 Quick Reflection", Content = "Write one thing you learned and one question you still have.", SortOrder = 3, XpReward = 10 });
            db.Lessons.Add(lesson);
        }
        await db.SaveChangesAsync();
    }
}
