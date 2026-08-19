using Microsoft.AspNetCore.Identity;
namespace TmsApi.Infrastructure.Identity;
public class TmsUser : IdentityUser
{
public string FirstName { get; set; } = string.Empty;
public string LastName { get; set; } = string.Empty;
public string? Department { get; set; }
  // Filled only for users with the Student role.
  public int? StudentId { get; set; }
}