using AIClassroom.Models;

namespace AIClassroom.Services;

public sealed record GameChallengeQuestion(string Prompt, string[] Options, int CorrectIndex);

public sealed class GameChallengeService
{
    /// <summary>
    /// Builds the challenge for a game and shuffles each question's options using <paramref name="seed"/>.
    /// The same seed always yields the same order, so the server can rebuild the exact attempt on submit
    /// without trusting the client. (Authored data has the correct answer first; without shuffling, "pick the first option" scores ~100%.)
    /// </summary>
    public IReadOnlyList<GameChallengeQuestion> Create(Game game, int seed)
    {
        var baseQuestions = CreateUnshuffled(game);
        var result = new List<GameChallengeQuestion>(baseQuestions.Count);
        for (var i = 0; i < baseQuestions.Count; i++)
        {
            var q = baseQuestions[i];
            var rng = new Random(HashCode.Combine(seed, i));
            var order = Enumerable.Range(0, q.Options.Length).OrderBy(_ => rng.Next()).ToArray();
            var options = order.Select(idx => q.Options[idx]).ToArray();
            result.Add(new GameChallengeQuestion(q.Prompt, options, Array.IndexOf(order, q.CorrectIndex)));
        }
        return result;
    }

    private IReadOnlyList<GameChallengeQuestion> CreateUnshuffled(Game game) => game.GameType.ToLowerInvariant() switch
    {
        "classification" => Classification(game),
        "pattern" => Pattern(),
        "sequence" => Sequence(),
        "matching" => Matching(),
        "logic" => Logic(),
        "maze" => Maze(),
        "flowchart" => Flowchart(),
        "creative" => Creative(),
        "data" => Data(),
        "training" => Training(),
        "accuracy" => Accuracy(),
        "ethics" => Ethics(),
        "nlp" => Nlp(),
        "sentiment" => Sentiment(),
        "prompt" => Prompt(),
        "model" => Model(),
        "problem" => Problem(),
        _ => Classification(game)
    };

    public int Score(IReadOnlyList<int?> answers, IReadOnlyList<GameChallengeQuestion> questions, out int correct)
    {
        correct = 0;
        for (var i = 0; i < questions.Count; i++)
            if (i < answers.Count && answers[i].HasValue && answers[i]!.Value == questions[i].CorrectIndex) correct++;
        return questions.Count == 0 ? 0 : (int)Math.Round(correct * 100d / questions.Count);
    }

    private static GameChallengeQuestion Q(string prompt, params string[] values)
    {
        var split = values[0].Split("||", StringSplitOptions.None);
        return new GameChallengeQuestion(prompt, split, int.Parse(values[1]));
    }

    private static IReadOnlyList<GameChallengeQuestion> Classification(Game g) => new[]
    {
        Q("Which example clearly uses AI to recognize a face?", "Phone face unlock||A wall clock||A paper notebook||A ruler", "0"),
        Q("A voice assistant understanding your words is an example of...", "AI||A battery||A chair||A cable", "0"),
        Q("Which item is NOT an AI system by itself?", "Wooden spoon||Spam filter||Recommendation engine||Image recognizer", "0"),
        Q("A camera that detects a person is using...", "Computer vision||A calculator only||A printer||A speaker", "0"),
        Q("Sorting photos by the people in them is mainly...", "Classification||Charging||Printing||Typing", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Pattern() => new[]
    {
        Q("2, 4, 6, 8, ?", "10||11||12||14", "0"),
        Q("A, B, A, B, A, ?", "B||C||A||D", "0"),
        Q("3, 6, 9, 12, ?", "14||15||16||18", "1"),
        Q("Red, Blue, Green, Red, Blue, ?", "Yellow||Green||Red||Blue", "1"),
        Q("1, 2, 4, 8, ?", "10||12||14||16", "3")
    };
    private static IReadOnlyList<GameChallengeQuestion> Sequence() => new[]
    {
        Q("What should Robo do first to make a sandwich?", "Get the bread||Eat the sandwich||Put it away||Turn off the light", "0"),
        Q("Which instruction order is best for brushing teeth?", "Brush → rinse → put brush away||Rinse → sleep → brush||Put brush away → brush||Sleep → rinse", "0"),
        Q("A robot must reach a door. What is a good first step?", "Move toward the door||Celebrate||Delete the map||Stop forever", "0"),
        Q("Algorithms work best when instructions are...", "Clear and ordered||Random||Hidden||Contradictory", "0"),
        Q("If a step depends on a previous step, you should...", "Do them in the correct order||Skip both||Swap randomly||Repeat forever", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Matching() => new[]
    {
        Q("Which sensor is best for seeing an image?", "Camera||Microphone||Temperature sensor||Touch sensor", "0"),
        Q("Which sensor detects sound?", "Camera||Microphone||Light sensor||GPS", "1"),
        Q("Which sensor measures heat?", "Temperature sensor||Camera||Speaker||Keyboard", "0"),
        Q("Which sensor can detect contact?", "Touch sensor||Microphone||Printer||Screen", "0"),
        Q("Which tool can help locate a device outdoors?", "GPS||Pencil||Speaker||Mouse", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Logic() => new[]
    {
        Q("A helpful chatbot should answer a clear question with...", "A relevant response||A random password||Silence||A broken link", "0"),
        Q("If a chatbot does not know an answer, the safest choice is to...", "Say it is unsure||Make up a fact||Blame the user||Hide the message", "0"),
        Q("Which is a good chatbot instruction?", "Explain in simple steps||Always confuse me||Never answer||Use random words", "0"),
        Q("A chatbot can be useful for...", "Answering common questions||Replacing every human decision||Guaranteeing truth||Reading minds", "0"),
        Q("Before trusting an AI answer, you should...", "Check important facts||Assume it is always correct||Share passwords||Ignore context", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Maze() => new[]
    {
        Q("Robo is at START. Which plan reaches the goal in a simple straight maze?", "Forward, Forward, Forward||Left forever||Backward forever||Stop", "0"),
        Q("What makes a maze algorithm easier to follow?", "Numbered steps||Random actions||Missing steps||Contradictions", "0"),
        Q("If Robo hits a wall, what should a decision step do?", "Choose another allowed path||Ignore the wall||Break the maze||Stop learning", "0"),
        Q("A repeat block is useful when...", "The same action happens several times||Nothing repeats||There is no path||You want random moves", "0"),
        Q("The final instruction in a maze should usually be...", "Reach the goal||Restart forever||Erase the plan||Turn off the map", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Flowchart() => new[]
    {
        Q("A flowchart diamond usually represents a...", "Decision||Start screen||Picture||Sound", "0"),
        Q("If answer is YES, a decision should...", "Follow the YES branch||Ignore both branches||Delete the question||Restart", "0"),
        Q("A process box usually represents...", "An action||A question mark only||A password||A score", "0"),
        Q("A flowchart is useful because it shows...", "Steps and decisions||Only colors||Only sounds||Only passwords", "0"),
        Q("For a yes/no decision, how many main branches are common?", "2||1||3||10", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Creative() => new[]
    {
        Q("Which prompt is most specific for creating an image?", "A robot in a blue classroom at sunrise||Robot||Make something||Picture", "0"),
        Q("Adding a style to a prompt can help control...", "How the result looks||The internet speed||The keyboard||Battery level", "0"),
        Q("Which prompt gives the clearest scene?", "A small green robot reading under a tree||Make art||Robot please||Something nice", "0"),
        Q("If an AI image is wrong, a good next step is to...", "Refine the prompt||Give no details||Delete everything||Stop learning", "0"),
        Q("A prompt is best thought of as...", "Instructions for an AI system||A computer cable||A battery||A screen", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Data() => new[]
    {
        Q("Which dataset is most useful for predicting favorite fruit?", "Fruit choices with labels||Random colors||Empty rows||Only passwords", "0"),
        Q("A recommendation system learns from...", "Patterns in data||Nothing||Only screen size||Only battery", "0"),
        Q("If many users like science videos, a recommender may...", "Recommend more science videos||Delete all videos||Turn off Wi-Fi||Change the keyboard", "0"),
        Q("Duplicate records can make data...", "Messier||Perfect||Smaller automatically||Invisible", "0"),
        Q("Good recommendations should consider...", "Relevant user data||Random guesses only||No information||Passwords", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Training() => new[]
    {
        Q("In supervised learning, training examples usually have...", "Labels||No data||Only colors||Passwords", "0"),
        Q("If all training examples are cats, the model may struggle with...", "Dogs||More cats||The same examples||The labels", "0"),
        Q("A model improves by learning patterns from...", "Training data||A blank screen||A speaker||A cable", "0"),
        Q("What should you do when training data has an obvious mistake?", "Review and fix it||Ignore it always||Duplicate it||Hide it", "0"),
        Q("Testing a model on new examples helps check...", "Generalization||Keyboard speed||Screen brightness||Battery", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Accuracy() => new[]
    {
        Q("A model gets 8 correct out of 10. Accuracy is...", "80%||20%||8%||100%", "0"),
        Q("A model gets 9 correct out of 12. Accuracy is...", "75%||25%||90%||60%", "0"),
        Q("A model gets 5 correct out of 5. Accuracy is...", "100%||50%||5%||0%", "0"),
        Q("A model gets 6 correct out of 10. Accuracy is...", "60%||40%||16%||90%", "0"),
        Q("Accuracy compares correct predictions with...", "Total predictions||Screen size||Dataset color||Training time only", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Ethics() => new[]
    {
        Q("If a dataset leaves out one group, the model may become...", "Biased||Perfect||Faster only||Smaller only", "0"),
        Q("A fair dataset should aim to...", "Represent relevant groups fairly||Exclude people randomly||Hide errors||Use one example", "0"),
        Q("What should you do when an AI result seems unfair?", "Investigate the data and model||Ignore it||Celebrate it||Delete the evidence", "0"),
        Q("Bias can enter an AI system through...", "Training data||Only the monitor||Only the keyboard||Only the speaker", "0"),
        Q("A good AI project should consider...", "Fairness and safety||Only speed||Only colors||Only game points", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Nlp() => new[]
    {
        Q("NLP helps computers work with...", "Human language||Batteries||Cables||Screens", "0"),
        Q("Which task is an NLP task?", "Text classification||Changing a light bulb||Charging a phone||Printing paper", "0"),
        Q("A chatbot mainly uses NLP to understand...", "Language||Temperature||Battery voltage||Screen size", "0"),
        Q("Breaking text into smaller pieces can help a model...", "Process language||Charge faster||Print better||Cool down", "0"),
        Q("Sentiment analysis is often used to identify...", "Opinion or emotion||GPS position||Battery level||Camera focus", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Sentiment() => new[]
    {
        Q("“I love this game!” is most likely...", "Positive||Negative||Neutral||Unknown", "0"),
        Q("“This lesson is terrible.” is most likely...", "Positive||Negative||Neutral||Unknown", "1"),
        Q("“The robot is on the table.” is usually...", "Positive||Negative||Neutral||Unknown", "2"),
        Q("“Amazing work!” is most likely...", "Positive||Negative||Neutral||Unknown", "0"),
        Q("“I do not like this.” is most likely...", "Positive||Negative||Neutral||Unknown", "1")
    };
    private static IReadOnlyList<GameChallengeQuestion> Prompt() => new[]
    {
        Q("Which prompt is most useful for a short science explanation?", "Explain photosynthesis for a Class 6 student in 5 bullet points||Explain||Science||Photosynthesis", "0"),
        Q("A good prompt should include...", "Useful context and a clear goal||Only one word||A password||Random symbols", "0"),
        Q("To make an answer shorter, you can ask for...", "A specific length or format||More randomness||No topic||A blank response", "0"),
        Q("To improve an unclear AI response, you should...", "Add constraints and examples||Remove all context||Use fewer details always||Stop", "0"),
        Q("Which instruction is clearest?", "Return 3 beginner-friendly ideas in a numbered list||Give stuff||Help||Ideas", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Model() => new[]
    {
        Q("In a simple AI pipeline, input goes to a model and then to...", "Output||A wall||A battery||A cable", "0"),
        Q("An image can be an AI system's...", "Input||Only output||Password||Error", "0"),
        Q("A trained model is used to...", "Make predictions||Charge a phone||Print a page||Play music only", "0"),
        Q("The prediction produced by a model is an...", "Output||Input||Sensor||Dataset", "0"),
        Q("A model needs useful input to produce a useful...", "Output||Battery||Keyboard||Cable", "0")
    };
    private static IReadOnlyList<GameChallengeQuestion> Problem() => new[]
    {
        Q("The school wants to detect attendance from a camera. Which AI area fits?", "Computer vision||Audio mixing||Word processing||Battery control", "0"),
        Q("To predict which books students may like, you could use...", "Recommendation or ML||A flashlight||A printer||A keyboard", "0"),
        Q("Before building an AI solution, first define...", "The problem and goal||The logo color only||A password||A random output", "0"),
        Q("A safe AI solution should protect...", "People and their data||Only the screen||Only the keyboard||Nothing", "0"),
        Q("After building a model, you should evaluate it with...", "Relevant test data||Only the training examples||A blank page||A random password", "0")
    };
}
