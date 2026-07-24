using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Authentication;

/// <summary>
/// Password hashing backed by BCrypt with a per-hash salt and work factor.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
