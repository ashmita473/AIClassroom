namespace AIClassroom.Models.Entities;

public class StudentProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CurriculumDayId { get; set; }
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
