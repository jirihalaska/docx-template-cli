namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Exception thrown when a template file cannot be found
/// </summary>
public class TemplateNotFoundException : DocxTemplateException
{
    /// <summary>
    /// Initializes a new instance of the TemplateNotFoundException class
    /// </summary>
    public TemplateNotFoundException() : base("Template file not found")
    {
    }

    /// <summary>
    /// Path to the template file that was not found
    /// </summary>
    public string? TemplatePath { get; }

    /// <summary>
    /// Initializes a new instance of the TemplateNotFoundException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public TemplateNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the TemplateNotFoundException class with a template path
    /// </summary>
    /// <param name="templatePath">Path to the template file that was not found</param>
    public TemplateNotFoundException(string templatePath, bool isPath) 
        : base($"Template file not found: {templatePath}")
    {
        TemplatePath = templatePath;
    }

    /// <summary>
    /// Initializes a new instance of the TemplateNotFoundException class with a template path and inner exception
    /// </summary>
    /// <param name="templatePath">Path to the template file that was not found</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public TemplateNotFoundException(string templatePath, Exception innerException) 
        : base($"Template file not found: {templatePath}", innerException)
    {
        TemplatePath = templatePath;
    }

    /// <summary>
    /// Creates a TemplateNotFoundException for a specific file path
    /// </summary>
    /// <param name="filePath">Path to the file that was not found</param>
    /// <returns>TemplateNotFoundException instance</returns>
    public static TemplateNotFoundException ForFile(string filePath)
    {
        return new TemplateNotFoundException(filePath, true);
    }

    /// <summary>
    /// Creates a TemplateNotFoundException for a directory
    /// </summary>
    /// <param name="directoryPath">Path to the directory that was not found</param>
    /// <returns>TemplateNotFoundException instance</returns>
    public static TemplateNotFoundException ForDirectory(string directoryPath)
    {
        return new TemplateNotFoundException(directoryPath, new DirectoryNotFoundException($"Template directory not found: {directoryPath}"));
    }
}