namespace Lld.Contracts;

/// <summary>
/// Basic identity of whichever side answered: the API (environment = ASP.NET environment)
/// or the native app shell (environment = device platform).
/// </summary>
public sealed record AppInfo(string Name, string Version, string Environment);
