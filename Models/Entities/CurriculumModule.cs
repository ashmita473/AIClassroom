namespace AIClassroom.Models.Entities;

public class CurriculumModule
{
    public int Id { get; set; }
    public string GroupName { get; set; } = "";
    public int ClassMin { get; set; }
    public int ClassMax { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public ICollection<CurriculumDay> Days { get; set; } = new List<CurriculumDay>();
}
