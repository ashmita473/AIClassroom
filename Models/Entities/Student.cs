namespace AIClassroom.Models.Entities;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public string StudentPin { get; set; } = "";
    public string GroupName { get; set; } = "";
    public int ClassNumber { get; set; }
    public int TotalXp { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
