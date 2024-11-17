using StudentTeacherManagement.Core.Models;

namespace StudentTeacherManagement.Core.Interfaces;

public interface IAuthService
{
    Task Register(User user, UserRole role);
    Task<(User?, string)> Login(string email, string password);

    Task<(User, string)> ValidateAccount(string email, int code);
    Task SendRecoveryCode(Guid id);
    Task SetNewPassword(Guid id, int code, string newPassword);
}