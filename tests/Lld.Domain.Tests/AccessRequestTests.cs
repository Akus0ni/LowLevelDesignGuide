using FluentAssertions;
using Lld.Domain.Access;
using Lld.Domain.Identity;

namespace Lld.Domain.Tests;

public sealed class AccessRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly EmailAddress Email = EmailAddress.Create("learner@example.com").Value;

    [Fact]
    public void Submit_CreatesPendingRequestAndRaisesEvent()
    {
        var request = AccessRequest.Submit(Email, " Ada ", " Please ", Now).Value;

        request.Status.Should().Be(AccessRequestStatus.Pending);
        request.Name.Should().Be("Ada");
        request.Note.Should().Be("Please");
        request.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AccessRequestSubmitted>();
    }

    [Fact]
    public void Submit_TreatsBlankNoteAsMissing()
    {
        AccessRequest.Submit(Email, "Ada", "   ", Now).Value.Note.Should().BeNull();
    }

    [Theory]
    [InlineData("", "validation.name.required")]
    [InlineData(null, "validation.name.required")]
    public void Submit_RequiresName(string? name, string expectedCode)
    {
        AccessRequest.Submit(Email, name, null, Now).Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Submit_RejectsOverlongNote()
    {
        var note = new string('x', AccessRequest.MaxNoteLength + 1);

        AccessRequest.Submit(Email, "Ada", note, Now).Error!.Code.Should().Be("validation.note.too_long");
    }

    [Fact]
    public void Merge_UpdatesPendingRequest()
    {
        var request = AccessRequest.Submit(Email, "Ada", null, Now).Value;

        var result = request.Merge("Ada L.", "Second try", Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        request.Name.Should().Be("Ada L.");
        request.Note.Should().Be("Second try");
        request.UpdatedAt.Should().Be(Now.AddHours(1));
        request.DomainEvents.Should().Contain(e => e is AccessRequestMerged);
    }

    [Fact]
    public void Approve_MovesPendingToApproved()
    {
        var request = AccessRequest.Submit(Email, "Ada", null, Now).Value;

        request.Approve(Now.AddMinutes(5)).IsSuccess.Should().BeTrue();

        request.Status.Should().Be(AccessRequestStatus.Approved);
        request.DecidedAt.Should().Be(Now.AddMinutes(5));
        request.DomainEvents.Should().Contain(e => e is AccessRequestApproved);
    }

    [Fact]
    public void Reject_MovesPendingToRejected()
    {
        var request = AccessRequest.Submit(Email, "Ada", null, Now).Value;

        request.Reject(Now).IsSuccess.Should().BeTrue();

        request.Status.Should().Be(AccessRequestStatus.Rejected);
        request.DomainEvents.Should().Contain(e => e is AccessRequestRejected);
    }

    public static TheoryData<Func<AccessRequest, Lld.Domain.Common.Result>> Transitions => new()
    {
        r => r.Approve(Now),
        r => r.Reject(Now),
        r => r.Merge("Someone", null, Now),
    };

    [Theory]
    [MemberData(nameof(Transitions))]
    public void DecidedRequests_RefuseEveryTransition(Func<AccessRequest, Lld.Domain.Common.Result> transition)
    {
        var approved = AccessRequest.Submit(Email, "Ada", null, Now).Value;
        approved.Approve(Now);
        var rejected = AccessRequest.Submit(Email, "Ada", null, Now).Value;
        rejected.Reject(Now);

        transition(approved).Error!.Code.Should().Be("access_request.not_pending");
        transition(rejected).Error!.Code.Should().Be("access_request.not_pending");
        approved.Status.Should().Be(AccessRequestStatus.Approved);
        rejected.Status.Should().Be(AccessRequestStatus.Rejected);
    }
}
