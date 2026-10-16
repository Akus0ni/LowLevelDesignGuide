namespace Lld.App.Data.Content;

/// <summary>Where the bundled pack comes from: MAUI app-package assets on the device, plain files in tests.</summary>
public interface IContentPackSource
{
    /// <summary>The bundled pack's version (a few bytes; read on every launch).</summary>
    Task<string> ReadVersionAsync(CancellationToken cancellationToken);

    /// <summary>The whole pack; opened only when the version differs from the loaded one.</summary>
    Task<Stream> OpenPackAsync(CancellationToken cancellationToken);
}
