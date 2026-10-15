using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Lld.ServerStatus.Tests;

public sealed class SignatureTests
{
    public static TheoryData<string> CaseNames() => new(TestVector.Load().Cases.Select(c => c.Name));

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void SharedTestVector_IsVerifiedAsExpected(string name)
    {
        var vector = TestVector.Load();
        var testCase = vector.Cases.Single(c => c.Name == name);
        using var verifier = StatusVerifier.FromSubjectPublicKeyInfo(vector.PublicKey);

        var result = verifier.Verify(testCase.Envelope);

        if (testCase.Expect == "valid")
        {
            result.IsValid.Should().BeTrue();
            result.Message!.Status.Should().Be(testCase.Status == "online" ? ServerState.Online : ServerState.Offline);
            result.Message.Reason.Should().Be(testCase.Reason);
            result.Message.At.Should().Be(ParseDate(testCase.At));
            result.Message.QuotaResetsAt.Should().Be(testCase.QuotaResetsAt is null ? null : ParseDate(testCase.QuotaResetsAt));
        }
        else
        {
            result.IsValid.Should().BeFalse();
            JsonNamingPolicy.SnakeCaseLower.ConvertName(result.Rejection!.Value.ToString()).Should().Be(testCase.Expect);
        }
    }

    [Fact]
    public void SignThenVerify_RoundTripsTheMessage()
    {
        var keys = StatusKeyPair.Create();
        using var signer = StatusSigner.FromPkcs8(keys.PrivateKeyPkcs8);
        using var verifier = StatusVerifier.FromSubjectPublicKeyInfo(keys.PublicKeySpki);
        var message = StatusMessage.Create(ServerState.Offline, StatusReasons.Shutdown, DateTimeOffset.UtcNow);

        var result = verifier.Verify(signer.Sign(message));

        result.Message.Should().Be(message);
    }

    [Fact]
    public void Payload_UsesCamelCaseAndLowercaseStatus()
    {
        var keys = StatusKeyPair.Create();
        using var signer = StatusSigner.FromPkcs8(keys.PrivateKeyPkcs8);
        var message = StatusMessage.Create(ServerState.Online, StatusReasons.Heartbeat, DateTimeOffset.UnixEpoch);

        using var envelope = JsonDocument.Parse(signer.Sign(message));
        var payload = Encoding.UTF8.GetString(
            Base64Url.DecodeFromChars(envelope.RootElement.GetProperty("payload").GetString()));

        payload.Should().Be("""{"v":1,"status":"online","reason":"heartbeat","at":"1970-01-01T00:00:00+00:00"}""");
    }

    private static DateTimeOffset ParseDate(string? text) => DateTimeOffset.Parse(text!, CultureInfo.InvariantCulture);
}

internal sealed record TestVectorCase(string Name, string Envelope, string Expect, string? Status, string? Reason, string? At, string? QuotaResetsAt);

internal sealed record TestVector(string PublicKey, IReadOnlyList<TestVectorCase> Cases)
{
    public static TestVector Load()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "test-vector.json"));
        return JsonSerializer.Deserialize<TestVector>(json, JsonSerializerOptions.Web)!;
    }
}
