namespace AIClassroom.Models;

public class CurriculumModuleEditViewModel
{
    public int? Id { get; set; }
    public string GroupName { get; set; } = "A";
    public int ClassMin { get; set; } = 3;
    public int ClassMax { get; set; } = 5;
    public string Code { get; set; } = "A1";
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
}

public class CurriculumDayEditViewModel
{
    public int? Id { get; set; }
    public int CurriculumModuleId { get; set; }
    public int DayNumber { get; set; } = 1;
    public string Title { get; set; } = "";
    public string ContentSummary { get; set; } = "";
    public int XpReward { get; set; } = 50;
    public bool IsPublished { get; set; } = true;
    public bool IsLocked { get; set; }
}

public class LessonEditorViewModel
{
    public int CurriculumDayId { get; set; }
    public string DayTitle { get; set; } = "";
    public int XpReward { get; set; } = 50;
    public string Introduction { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Examples { get; set; } = "";
    public string TeacherNotes { get; set; } = "";
    public string StudentNotesTemplate { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public List<LessonActivityEditViewModel> Activities { get; set; } = new();
    public List<LessonVideoEditViewModel> Videos { get; set; } = new();
    public List<LinkedQuizViewModel> AvailableQuizzes { get; set; } = new();
    public List<int> SelectedQuizIds { get; set; } = new();
}

public class LessonActivityEditViewModel
{
    public int? Id { get; set; }
    public string ActivityType { get; set; } = "Question";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int SortOrder { get; set; }
    public int XpReward { get; set; } = 10;
    public bool Delete { get; set; }
}

public class LinkedQuizViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsPublished { get; set; }
    public int QuestionCount { get; set; }
}

public class LessonVideoEditViewModel
{
    public int? Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string YouTubeUrl { get; set; } = "";
    public string VideoId { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public int XpReward { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool Delete { get; set; }
}
