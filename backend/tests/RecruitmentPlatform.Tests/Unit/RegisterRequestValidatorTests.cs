using FluentAssertions;
using FluentValidation.TestHelper;
using RecruitmentPlatform.Application.DTOs.Auth;
using RecruitmentPlatform.Application.Validators.Auth;
using Xunit;

namespace RecruitmentPlatform.Tests.Unit;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new(
        "Ada", "Lovelace", "ada@example.com", "Passw0rd!", "Passw0rd!", "+15551234567", "Candidate");

    [Fact]
    public void Passes_ForValidCandidateRegistration()
    {
        _validator.TestValidate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]      // too short
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")]  // no digit
    public void Fails_ForWeakPassword(string password)
    {
        var request = Valid() with { Password = password, ConfirmPassword = password };
        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Fails_WhenPasswordsDoNotMatch()
    {
        var request = Valid() with { ConfirmPassword = "Different1!" };
        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Fails_ForInvalidEmail()
    {
        var request = Valid() with { Email = "not-an-email" };
        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Fails_WhenSelfRegisteringPrivilegedRole()
    {
        var request = Valid() with { Role = "Administrator" };
        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Role);
    }
}
