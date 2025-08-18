namespace DocxTemplate.UI.Services;

public class CliCommandBuilder
{
    public string BuildListSetsCommand(string templatesPath)
    {
        return $"list-sets --templates \"{templatesPath}\" --format json";
    }

    public string BuildScanCommand(string path)
    {
        return $"scan --path \"{path}\" --format json";
    }

    public string BuildCopyCommand(string source, string target)
    {
        return $"copy --source \"{source}\" --target \"{target}\" --format json";
    }

    public string BuildReplaceCommand(string folder, string mapFile)
    {
        return $"replace --folder \"{folder}\" --map \"{mapFile}\" --format json";
    }
}