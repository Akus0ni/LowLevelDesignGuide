using System.Buffers.Text;
using System.Text.Json;

namespace Lld.ServerStatus;

/// <summary>Wire format helpers: camelCase JSON (source-generated, trim-safe) and base64url.</summary>
internal static class StatusCodec
{
    public static byte[] SerializePayload(StatusMessage message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, StatusJsonContext.Default.StatusMessage);

    public static string SerializeEnvelope(SignedEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, StatusJsonContext.Default.SignedEnvelope);

    public static string ToBase64Url(byte[] bytes) => Base64Url.EncodeToString(bytes);

    public static bool TryReadEnvelope(string json, out byte[] payload, out byte[] signature)
    {
        payload = [];
        signature = [];
        try
        {
            var envelope = JsonSerializer.Deserialize(json, StatusJsonContext.Default.SignedEnvelope);
            if (envelope?.Payload is null || envelope.Sig is null)
            {
                return false;
            }

            payload = Base64Url.DecodeFromChars(envelope.Payload);
            signature = Base64Url.DecodeFromChars(envelope.Sig);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return false;
        }
    }

    public static StatusMessage? TryReadPayload(byte[] payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize(payload, StatusJsonContext.Default.StatusMessage);
            return message is { Reason: not null } ? message : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
