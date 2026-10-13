using ChatApp.Domain.Entities;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Application.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Nickname_is_trimmed()
    {
        var user = new User("  karem  ");

        Assert.Equal("karem", user.Nickname);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("this-nickname-is-way-too-long-for-the-app")]
    [InlineData("bad<name>")]
    public void Invalid_nicknames_are_rejected(string nickname)
    {
        Assert.Throws<DomainException>(() => new User(nickname));
    }
}
