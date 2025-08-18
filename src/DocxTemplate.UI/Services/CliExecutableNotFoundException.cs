using System;

namespace DocxTemplate.UI.Services;

public class CliExecutableNotFoundException : Exception
{
    public string SearchDirectory { get; }
    
    public CliExecutableNotFoundException(string searchDirectory) 
        : base($"CLI executable not found in directory: {searchDirectory}. " +
               "Please ensure 'docx-template.exe' (Windows) or 'docx-template' (macOS/Linux) is in the same directory as the GUI application.")
    {
        SearchDirectory = searchDirectory;
    }
    
    public CliExecutableNotFoundException(string searchDirectory, string message) 
        : base(message)
    {
        SearchDirectory = searchDirectory;
    }
    
    public CliExecutableNotFoundException(string searchDirectory, string message, Exception innerException) 
        : base(message, innerException)
    {
        SearchDirectory = searchDirectory;
    }
}