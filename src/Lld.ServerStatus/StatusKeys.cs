using System.Security.Cryptography;

namespace Lld.ServerStatus;

/// <summary>An ECDSA P-256 key pair in portable formats: PKCS#8 (private, stays on the PC) and SPKI (public, ships in the app).</summary>
public sealed record StatusKeyPair(string PrivateKeyPkcs8, string PublicKeySpki)
{
    public static StatusKeyPair Create()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new StatusKeyPair(
            Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
    }
}

/// <summary>Signs status messages on the PC. The signature uses IEEE P1363 (r||s), the format WebCrypto expects.</summary>
public sealed class StatusSigner : IDisposable
{
    private readonly ECDsa _key;

    private StatusSigner(ECDsa key) => _key = key;

    public static StatusSigner FromPkcs8(string privateKeyBase64)
    {
        var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
        return new StatusSigner(key);
    }

    /// <summary>Returns the envelope JSON to publish as the ntfy message body.</summary>
    public string Sign(StatusMessage message)
    {
        var payload = StatusCodec.SerializePayload(message);
        var signature = _key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return StatusCodec.SerializeEnvelope(new SignedEnvelope(StatusCodec.ToBase64Url(payload), StatusCodec.ToBase64Url(signature)));
    }

    public void Dispose() => _key.Dispose();
}
