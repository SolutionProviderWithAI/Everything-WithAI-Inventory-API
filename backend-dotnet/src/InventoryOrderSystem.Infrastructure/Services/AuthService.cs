using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usernameOrEmail = request.UsernameOrEmail.Trim().ToLower();
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameOrEmail || u.Email.ToLower() == usernameOrEmail, ct);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleValidationException("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleValidationException("Account is deactivated. Please contact support.");
        }

        var expiresAt = DateTime.UtcNow.AddHours(12);
        var token = GenerateJwtToken(user, expiresAt);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.Name,
                IsActive = user.IsActive,
                CustomerId = user.CustomerProfile?.Id
            }
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLower();
        var username = request.Username.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
        {
            throw new BusinessRuleValidationException($"User with email '{request.Email}' already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username, ct))
        {
            throw new BusinessRuleValidationException($"User with username '{request.Username}' already exists.");
        }

        var roleName = string.IsNullOrWhiteSpace(request.RoleName) ? "Customer" : request.RoleName.Trim();
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);
        if (role == null)
        {
            role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer", ct)
                ?? throw new NotFoundException(nameof(Role), roleName);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber,
            PasswordHash = HashPassword(request.Password),
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user, ct);

        // If customer role, also create linked customer profile
        Customer? cust = null;
        if (role.Name.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            cust = new Customer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CustomerCode = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                Name = user.FullName,
                Email = user.Email,
                Phone = user.PhoneNumber ?? "+91 9999999999",
                CreditLimit = 50000m,
                OutstandingBalance = 0m,
                IsActive = true
            };
            await _context.Customers.AddAsync(cust, ct);
        }

        await _context.SaveChangesAsync(ct);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = role.Name,
            IsActive = user.IsActive,
            CustomerId = cust?.Id
        };
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.CustomerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.Name,
            IsActive = user.IsActive,
            CustomerId = user.CustomerProfile?.Id
        };
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.CustomerProfile)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.Name,
                IsActive = u.IsActive,
                CustomerId = u.CustomerProfile != null ? u.CustomerProfile.Id : null
            })
            .ToListAsync(ct);
    }

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForDevelopmentAndProductionSecurityTesting1234567890!@#$%";
        var issuer = _configuration["Jwt:Issuer"] ?? "InventoryOrderSystem";
        var audience = _configuration["Jwt:Audience"] ?? "InventoryOrderClients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name),
            new("FullName", user.FullName)
        };

        if (user.CustomerProfile != null)
        {
            claims.Add(new("CustomerId", user.CustomerProfile.Id.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
