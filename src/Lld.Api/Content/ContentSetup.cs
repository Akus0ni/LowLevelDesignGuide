using Lld.Content.Authoring;

namespace Lld.Api.Content;

internal static class ContentSetup
{
    /// <summary>Path of the content folder; Aspire sets it, otherwise the repository is found from the API's folder.</summary>
    public const string PathKey = "Content:Path";

    private const string SolutionFile = "LowLevelDesignGuide.slnx";

    /// <summary>Registers the dev content source and its search index, only in Development (the hosted API serves no content).</summary>
    public static IServiceCollection AddLldDevContent(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        var contentFolder = configuration[PathKey] is { Length: > 0 } configured
            ? Path.GetFullPath(configured)
            : Path.Combine(FindRepositoryRoot(environment.ContentRootPath), new ContentOptions().ContentDirectory);
        var repositoryRoot = Path.GetDirectoryName(contentFolder)
            ?? throw new InvalidOperationException($"{PathKey} must be a folder inside the repository.");
        var options = new ContentOptions { ContentDirectory = Path.GetFileName(contentFolder) };
        services.AddSingleton(new DevContentCatalog(repositoryRoot, options));
        services.AddSingleton<DevSearchIndex>();
        return services;
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException($"Set {PathKey}: {SolutionFile} was not found above {start}.");
    }
}
