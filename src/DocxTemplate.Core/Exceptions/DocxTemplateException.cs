namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Base exception class for all DocxTemplate-specific exceptions
/// </summary>
public abstract class DocxTemplateException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DocxTemplateException class
    /// </summary>
    protected DocxTemplateException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DocxTemplateException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    protected DocxTemplateException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DocxTemplateException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    protected DocxTemplateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}