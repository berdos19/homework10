namespace StudentTeacherManagement.DTOs;

public class RecoveryDataDTO
{
    public Guid Id { get; set; }
    public int Code { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}
