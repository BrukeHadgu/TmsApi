using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
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

    public record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    [EndpointSummary("Register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
            return Ok(new { message = "Registration request received." });

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
            return BadRequest(new
            {
                errors = createResult.Errors.Select(e => e.Description)
            });
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

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = updateResult.Errors.Select(e => e.Description)
                });
            }
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            var roleResult =
                await _roleManager.CreateAsync(
                    new IdentityRole(request.Role));

            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = roleResult.Errors.Select(e => e.Description)
                });
            }
        }

        var roleAssignmentResult =
            await _userManager.AddToRoleAsync(user, request.Role);

        if (!roleAssignmentResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = roleAssignmentResult.Errors.Select(e => e.Description)
            });
        }
        await transaction.CommitAsync(ct);
        return Ok(new
        {
            message = "Registration successful.",
            registrationNumber
        });
    }


    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("login")]
    [EndpointSummary("Login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var user =
            await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });

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

        var roles =
            await _userManager.GetRolesAsync(user);

        // Generate access token
        var accessToken =
            _tokenService.GenerateJwt(user, roles);

        // Generate refresh token
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
        Response.Cookies.Append(
        "refreshToken",refreshToken.Token,
            new CookieOptions
            {
                HttpOnly = true,
                // Production HTTPS/secure = true.
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires =
                    DateTimeOffset.UtcNow.AddDays(7),
                Path = "/api/auth"
            });
        // only return the access token 
        return Ok(new
        {
            accessToken
        });
    }

    [HttpPost("refresh")]
    [EndpointSummary("Refresh Token")]
    public async Task<IActionResult> Refresh()
    {
        // Read refresh token from HttpOnly cookie
        if (!Request.Cookies.TryGetValue(
                "refreshToken",
                out var refreshToken))
        {
            return Unauthorized(new
            {
                detail = "Refresh token cookie is missing."
            });
        }

        var storedToken =
            await _db.RefreshTokens
                .FirstOrDefaultAsync(
                    t => t.Token == refreshToken);

        if (storedToken is null)
        {
            return Unauthorized(new
            {
                detail = "Invalid refresh token."
            });
        }

        if (storedToken.IsUsed)
        {
            // Token reuse detected.
            // Revoke all refresh tokens belonging to this user.

            var userTokens =
                await _db.RefreshTokens
                    .Where(t => t.UserId == storedToken.UserId)
                    .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
            }

            await _db.SaveChangesAsync();

            Response.Cookies.Delete(
                "refreshToken",
                new CookieOptions
                {
                    Path = "/api/auth"
                });

            return Unauthorized(new
            {
                detail =
                    "Token theft detected. All user sessions revoked."
            });
        }

        if (storedToken.IsRevoked ||
            storedToken.ExpiresAt < DateTime.UtcNow)
        {
            Response.Cookies.Delete(
                "refreshToken",
                new CookieOptions
                {
                    Path = "/api/auth"
                });

            return Unauthorized(new
            {
                detail =
                    "Refresh token expired or revoked."
            });
        }

        storedToken.IsUsed = true;
        //create new refresh token  
        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _db.RefreshTokens.Add(newRefreshToken);
        //find user
        var user =
            await _userManager.FindByIdAsync(
                storedToken.UserId);

        if (user is null)
        {
            return Unauthorized(new
            {
                detail = "User account no longer exists."
            });
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        // Generate new access token
        var newAccessToken =
            _tokenService.GenerateJwt(user, roles);

        await _db.SaveChangesAsync();

        Response.Cookies.Append(
            "refreshToken",
            newRefreshToken.Token,
            new CookieOptions
            {
                HttpOnly = true,

                // Development:
                Secure = false,

                // Production HTTPS:
                // Secure = true

                SameSite = SameSiteMode.Lax,

                Expires =
                    DateTimeOffset.UtcNow.AddDays(7),

                Path = "/api/auth"
            });

        // Return ONLY the new access token
        return Ok(new
        {
            accessToken = newAccessToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    [EndpointSummary("Logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(
                "refreshToken",
                out var refreshToken))
        {
            var storedToken =
                await _db.RefreshTokens
                    .FirstOrDefaultAsync(
                        t => t.Token == refreshToken);

            if (storedToken is not null)
            {
                storedToken.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete(
            "refreshToken",
            new CookieOptions
            {
                Path = "/api/auth"
            });

        return Ok(new
        {
            message = "Logged out successfully."
        });
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Get Current User")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.FindByIdAsync(
            _userManager.GetUserId(User)!);

        if (user is null)
        {
            return Unauthorized(new
            {
                detail = "Session expired."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        string? registrationNumber = null;

        if (user.StudentId.HasValue)
        {
            registrationNumber = await _db.Students
                .AsNoTracking()
                .Where(student => student.Id == user.StudentId.Value)
                .Select(student => student.RegistrationNumber)
                .FirstOrDefaultAsync();
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            displayName = $"{user.FirstName} {user.LastName}",
            role = roles.FirstOrDefault() ?? "Student",
            studentId = user.StudentId,
            registrationNumber
        });
    }

    private async Task<string> GenerateRegistrationNumberAsync(
        CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"TMS-{year}-";

        var existingNumbers =
            await _db.Students
                .AsNoTracking()
                .Where(s =>
                    s.RegistrationNumber.StartsWith(prefix))
                .Select(s => s.RegistrationNumber)
                .ToListAsync(ct);

        var highestNumber =
            existingNumbers
                .Select(n =>
                {
                    var suffix = n[prefix.Length..];

                    return int.TryParse(
                        suffix,
                        out var parsed)
                        ? parsed
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

        return $"{prefix}{highestNumber + 1:0000}";
    }
}