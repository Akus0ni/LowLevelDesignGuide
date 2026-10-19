using FluentAssertions;
using Lld.Domain.Progress;

namespace Lld.Domain.Tests.Progress;

public sealed class ProgressOwnerTests
{
    [Fact]
    public void User_KeysNeverCollideWithTheGuest()
    {
        ProgressOwner.User("guest").Key.Should().NotBe(ProgressOwner.Guest.Key);
    }

    [Fact]
    public void User_ComparesByValue()
    {
        ProgressOwner.User("u1").Should().Be(ProgressOwner.User("u1"));
    }

    [Fact]
    public void Guest_HasNoUserId()
    {
        ProgressOwner.Guest.Should().Match<ProgressOwner>(o => o.IsGuest && o.UserId == null);
    }
}
