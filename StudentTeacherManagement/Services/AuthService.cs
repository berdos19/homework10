using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeaherManagement.Storage;

namespace StudentTeacherManagement.Services;

public class AuthService : IAuthService
{
    private const string PasswordPattern = @"^((?=\S*?[A-Z])(?=\S*?[a-z])(?=\S*?[0-9]).{6,})\S$";
    private const string EmailPattern = @"^[\w\.-]+@[a-zA-Z\d\.-]+\.[a-zA-Z]{2,6}$";
    private const int MinCodeValue = 100_000;
    private const int MaxCodeValue = 1_000_000;
    private static TimeSpan MaxVerificationTime => TimeSpan.FromMinutes(10);

    private static TimeSpan MaxRecoveryCodeLifeSpan = TimeSpan.FromMinutes(15);

    private static readonly Dictionary<int, CodeInfo> _recoveryCodes = new();

    private readonly DataContext _context;
    private readonly IEmailSender _emailSender;
    private IConfiguration _configuration;

    private static IDictionary<int, User> _unverifiedUsers = new Dictionary<int, User>();

    public AuthService(DataContext context, IEmailSender emailSender, IConfiguration configuration)
    {
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public Task Register(User user, UserRole role)
    {
        // validation
        ValidateUser(user);
        
        // generate code
        var code = new Random().Next(MinCodeValue, MaxCodeValue);
        user.CreatedAt = DateTime.UtcNow;
        user.Role = role;
        _unverifiedUsers.Add(code, user);
        
        // send to email
        return _emailSender.Send("Your code: " + code);
    }

    private void ValidateUser(User user)
    {
        if (string.IsNullOrEmpty(user.FirstName))
        {
            throw new ArgumentException("First name is invalid", nameof(user.FirstName));
        }
        if (string.IsNullOrEmpty(user.LastName))
        {
            throw new ArgumentException("Last name is invalid", nameof(user.LastName));
        }
        if (user.DateOfBirth > DateTime.Now)
        {
            throw new ArgumentException("Date of birth is invalid", nameof(user.DateOfBirth));
        }
        if (!Regex.IsMatch(user.Email, EmailPattern))
        {
            throw new ArgumentException("Email is invalid", nameof(user.Email));
        }   
        if (!Regex.IsMatch(user.Password, PasswordPattern))
        {
            throw new ArgumentException("Password is invalid", nameof(user.Password));
        }   
    }

    public async Task<(User?, string)> Login(string email, string password)
    {
        // check email and password
        var user = await _context.Students.SingleOrDefaultAsync(s => s.Email.Equals(email) && s.Password.Equals(password));
        return (user, GenerateToken(user));
        // [5, 4, 1, 43, 1, 2, 1]
        // First(1) => 1
        // Single(1) => exception
        // Single(4) => 4
    }

    public async Task<(User, string)> ValidateAccount(string email, int code)
    {
        // check code with email
        if (_unverifiedUsers.TryGetValue(code, out var unverifiedUser))
        {
            if (unverifiedUser.Email.Equals(email) && (DateTime.UtcNow - unverifiedUser.CreatedAt) < MaxVerificationTime)
            {
                var student = new Student()
                {
                    FirstName = unverifiedUser.FirstName,
                    LastName = unverifiedUser.LastName,
                    Email = unverifiedUser.Email,
                    Password = unverifiedUser.Password,
                    DateOfBirth = unverifiedUser.DateOfBirth,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                return (student, GenerateToken(student));
            }
        }

        throw new ArgumentException("Code or email is incorrect");
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("AppSettings:Token").Value!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration.GetSection("AppSettings:ExpireTime").Value!)),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    public async Task SendRecoveryCode(Guid id)
    {
        if (!await _context.Students.AnyAsync(s => s.Id == id))
        {
            throw new KeyNotFoundException(nameof(id));
        }

        var code = new Random().Next(MinCodeValue, MaxCodeValue);

        _recoveryCodes.Add(code, new CodeInfo
        {
            UserId = id,
            CreatedAt = DateTime.UtcNow
        });

        await _emailSender.Send($"Your code: " + code);
    }

    public async Task SetNewPassword(Guid id, int code, string newPassword)
    {
        if (_recoveryCodes.TryGetValue(code, out var recoveryData))
        {
            if (recoveryData.UserId == id && (DateTime.UtcNow - recoveryData.CreatedAt) < MaxRecoveryCodeLifeSpan)
            {
                var student = await _context.Students.FindAsync(id);

                if (student is not null)
                {
                    student.Password = newPassword;
                    await _context.SaveChangesAsync();
                    return;
                }
            }
        }

        throw new ArgumentException("Incoming code is incorrect");
    }
}