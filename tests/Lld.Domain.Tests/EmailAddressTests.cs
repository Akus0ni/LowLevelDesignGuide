using FluentAssertions;
using Lld.Domain.Identity;

namespace Lld.Domain.Tests;

public sealed class EmailAddressTests
{
    [Fact]
    public void Create_NormalisesCaseAndWhitespace()
    {
        var result = EmailAddress.Create("  Ada.Lovelace@Example.COM ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ada.lovelace@example.com");
    }

    [Fact]
    public void Create_TreatsDifferentCasingAsTheSameAddress()
    {
        EmailAddress.Create("A@B.io").Value.Should().Be(EmailAddress.Create("a@b.io").Value);
    }

    [Theory]
    [InlineData(null, "validation.email.required")]
    [InlineData("   ", "validation.email.required")]
    [InlineData("not-an-email", "validation.email.invalid")]
    [InlineData("user@localhost", "validation.email.invalid")]
    [InlineData("Ada <ada@example.com>", "validation.email.invalid")]
    public void Create_RejectsInvalidInput(string? input, string expectedCode)
    {
        var result = EmailAddress.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(expectedCode);
    }
}
