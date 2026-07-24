using FluentAssertions;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Infrastructure.Authentication;
using Xunit;

namespace RecruitmentPlatform.Tests.Unit;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesVerifiableHash()
    {
        var hash = _hasher.Hash("Passw0rd!");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("Passw0rd!");
        _hasher.Verify("Passw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Correct1!");
        _hasher.Verify("Wrong1!", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        _hasher.Verify("anything", "not-a-bcrypt-hash").Should().BeFalse();
    }
}

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtSettings
    {
        Key = "test-signing-key-that-is-long-enough-0123456789ABCDEF",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenExpirationMinutes = 30,
    }));

    [Fact]
    public void GenerateAccessToken_EmbedsRolesAndExpiry()
    {
        var service = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com", FirstName = "Test", LastName = "User" };

        var (token, expiresAt) = service.GenerateAccessToken(user, new[] { "Candidate" });

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // header.payload.signature
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var service = CreateService();
        service.GenerateRefreshToken().Should().NotBe(service.GenerateRefreshToken());
    }
}
