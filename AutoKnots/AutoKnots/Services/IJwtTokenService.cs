using Microsoft.AspNetCore.Identity;

namespace AutoKnots.Services;

public interface IJwtTokenService
{
    string GenerateToken(IdentityUser user);
}
