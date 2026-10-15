using System.Security.Cryptography;

namespace Lld.ServerStatus;

public enum StatusRejection
{
    Malformed,
    BadSignature,
    UnsupportedVersion,
}

/// <summary>Either a verified message or the reason it was rejected; rejections are expected, not exceptional.</summary>
public sealed record StatusVerification(StatusMessage? Message, StatusRejection? Rejection)
{
    public bool IsValid => Message is not null;

    public static StatusVerification Valid(StatusMessage message) => new(message, null);

    public static StatusVerification Rejected(StatusRejection reason) => new(null, reason);
}

/// <summary>
/// Verifies envelopes with the server's public key. The signature is checked on the raw payload bytes
/// before anything is parsed, so a forged message never reaches the JSON parser's output.
/// </summary>
public sealed class StatusVerifier : IDisposable
{
    private readonly ECDsa _key;

    private StatusVerifier(ECDsa key) => _key = key;

    public static StatusVerifier FromSubjectPublicKeyInfo(string publicKeyBase64)
    {
        var key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
        return new StatusVerifier(key);
    }

    public StatusVerification Verify(string envelopeJson)
    {
        if (!StatusCodec.TryReadEnvelope(envelopeJson, out var payload, out var signature))
        {
            return StatusVerification.Rejected(StatusRejection.Malformed);
        }

        if (!_key.VerifyData(payload, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            return StatusVerification.Rejected(StatusRejection.BadSignature);
        }

        var message = StatusCodec.TryReadPayload(payload);
        if (message is null)
        {
            return StatusVerification.Rejected(StatusRejection.Malformed);
        }

        return message.V == StatusMessage.CurrentVersion
            ? StatusVerification.Valid(message)
            : StatusVerification.Rejected(StatusRejection.UnsupportedVersion);
    }

    public void Dispose() => _key.Dispose();
}
