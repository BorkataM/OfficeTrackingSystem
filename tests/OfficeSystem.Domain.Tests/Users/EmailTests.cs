using FluentAssertions;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Domain.Tests.Users;

public sealed class EmailTests
{
    [Theory]
    [InlineData("ada@example.com", "ada@example.com")]
    [InlineData("  Ada@Example.COM  ", "ada@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk", "first.last+tag@sub.example.co.uk")]
    public void Create_NormalisesToLowerCaseAndTrims(string input, string expected)
    {
        Result<Email> result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsABlankAddress(string? input)
    {
        Email.Create(input).Error.Should().Be(UserErrors.EmailEmpty);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("ada@")]
    [InlineData("two@@example.com")]
    [InlineData("spaces in@example.com")]
    public void Create_RejectsAMalformedAddress(string input)
    {
        Email.Create(input).Error.Should().Be(UserErrors.EmailInvalid);
    }

    [Fact]
    public void Create_RejectsAnAddressOverTheLimit()
    {
        string tooLong = new string('a', Email.MaxLength) + "@example.com";

        Email.Create(tooLong).Error.Should().Be(UserErrors.EmailTooLong);
    }

    [Fact]
    public void Equality_IgnoresCaseAndSurroundingSpace()
    {
        Email left = Email.Create("ada@example.com").Value;
        Email right = Email.Create("  ADA@EXAMPLE.COM ").Value;

        left.Should().Be(right);
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }
}
