namespace DocxTemplate.UI.Services;

public class CliCommandBuilder
{
    public record CliCommand(string CommandName, string[] Arguments);

    public virtual CliCommand BuildListSetsCommand(string templatesPath)
    {
        return new CliCommand("list-sets", new[]
        {
            "--templates", templatesPath,
            "--format", "json"
        });
    }

    public virtual CliCommand BuildScanCommand(string path)
    {
        return new CliCommand("scan", new[]
        {
            "--path", path,
            "--format", "json"
        });
    }

    public virtual CliCommand BuildCopyCommand(string source, string target)
    {
        return new CliCommand("copy", new[]
        {
            "--source", source,
            "--target", target,
            "--format", "json"
        });
    }

    public virtual CliCommand BuildReplaceCommand(string folder, string mapFile)
    {
        return new CliCommand("replace", new[]
        {
            "--folder", folder,
            "--map", mapFile,
            "--format", "json"
        });
    }
}