namespace DocxTemplate.UI.Services;

public class CliCommandBuilder
{
    public record CliCommand(string CommandName, string[] Arguments);

    public virtual CliCommand BuildListSetsCommand(string templatesPath)
    {
        return new CliCommand("list-sets", new[]
        {
            "--templates", QuotePathIfNeeded(templatesPath),
            "--format", "json"
        });
    }

    public virtual CliCommand BuildScanCommand(string path)
    {
        return new CliCommand("scan", new[]
        {
            "--path", QuotePathIfNeeded(path),
            "--format", "json"
        });
    }

    public virtual CliCommand BuildCopyCommand(string source, string target)
    {
        return new CliCommand("copy", new[]
        {
            "--source", QuotePathIfNeeded(source),
            "--target", QuotePathIfNeeded(target),
            "--format", "json"
        });
    }

    public virtual CliCommand BuildReplaceCommand(string folder, string mapFile)
    {
        return new CliCommand("replace", new[]
        {
            "--folder", QuotePathIfNeeded(folder),
            "--map", QuotePathIfNeeded(mapFile),
            "--format", "json"
        });
    }

    private static string QuotePathIfNeeded(string path)
    {
        // Quote the path if it contains spaces and isn't already quoted
        if (string.IsNullOrEmpty(path))
            return path;
            
        if (path.Contains(' ') && !path.StartsWith('"') && !path.EndsWith('"'))
        {
            return $"\"{path}\"";
        }
        
        return path;
    }
}