using Lld.Content.Cli;

// lld-content: validates and packs content/ (app build, CI) and writes static API responses (Playwright).
// Usage: lld-content --help
return await ContentCli.RunAsync(args, Console.Out, Console.Error);
