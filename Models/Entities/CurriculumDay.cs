namespace AIClassroom.Models.Entities;

public class CurriculumDay
{
    public int Id { get; set; }
    public int CurriculumModuleId { get; set; }
    public CurriculumModule? CurriculumModule { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = "";
    public string ContentSummary { get; set; } = "";
    public bool IsPublished { get; set; }
    public bool IsLocked { get; set; }
}
