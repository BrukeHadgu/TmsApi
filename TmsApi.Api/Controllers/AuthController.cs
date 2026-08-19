using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TmsDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TmsDbContext db,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _tokenService = tokenService;
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            return Ok(new
            {
                message = "Registration request received."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var createResult =
            await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .Select(error => error.Description);

            return BadRequest(new { errors });
        }

        string? registrationNumber = null;

        if (string.Equals(
                request.Role,
                "Student",
                StringComparison.OrdinalIgnoreCase))
        {
            registrationNumber =
                await GenerateRegistrationNumberAsync(ct);

            var student = new Student
            {
                RegistrationNumber = registrationNumber,
                Name = $"{request.FirstName} {request.LastName}",
                GPA = 0m,
                IsActive = true
            };

            _db.Students.Add(student);
            await _db.SaveChangesAsync(ct);

            user.StudentId = student.Id;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors
                    .Select(error => error.Description);

                return BadRequest(new { errors });
            }
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            var roleResult =
                await _roleManager.CreateAsync(
                    new IdentityRole(request.Role));

            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors
                    .Select(error => error.Description);

                return BadRequest(new { errors });
            }
        }

        var roleAssignmentResult =
            await _userManager.AddToRoleAsync(user, request.Role);

        if (!roleAssignmentResult.Succeeded)
        {
            var errors = roleAssignmentResult.Errors
                .Select(error => error.Description);

            return BadRequest(new { errors });
        }

        await transaction.CommitAsync(ct);

        return Ok(new
        {
            message = "Registration successful.",
            registrationNumber
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new
            {
                detail =
                    "Account locked due to multiple failed login attempts."
            });
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);

            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _tokenService.GenerateJwt(user, roles);

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token
        });
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.Token == request.RefreshToken);

        if (storedToken is null)
        {
            return Unauthorized(new
            {
                detail = "Invalid refresh token."
            });
        }

        // A used token indicates replay/token-theft activity.
        if (storedToken.IsUsed)
        {
            var userTokens = await _db.RefreshTokens
                .Where(token => token.UserId == storedToken.UserId)
                .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
            }

            await _db.SaveChangesAsync();

            return Unauthorized(new
            {
                detail =
                    "Token theft detected. All user sessions revoked."
            });
        }

        if (
            storedToken.IsRevoked ||
            storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                detail = "Refresh token expired or revoked."
            });
        }

        storedToken.IsUsed = true;

        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(
            storedToken.UserId);

        if (user is null)
        {
            return Unauthorized(new
            {
                detail = "User account no longer exists."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var newAccessToken =
            _tokenService.GenerateJwt(user, roles);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    private async Task<string> GenerateRegistrationNumberAsync(
        CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"TMS-{year}-";

        var existingNumbers = await _db.Students
            .AsNoTracking()
            .Where(student =>
                student.RegistrationNumber.StartsWith(prefix))
            .Select(student => student.RegistrationNumber)
            .ToListAsync(ct);

        var highestNumber = existingNumbers
            .Select(number =>
            {
                var suffix = number[prefix.Length..];

                return int.TryParse(suffix, out var parsed)
                    ? parsed
                    : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{highestNumber + 1:0000}";
    }

    public record LoginRequest(string Email, string Password);
}