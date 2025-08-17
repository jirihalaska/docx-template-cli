namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Exception thrown when there's an error processing a Word document with OpenXml
/// </summary>
public class DocumentProcessingException : DocxTemplateException
{
    /// <summary>
    /// Path to the document that failed to process
    /// </summary>
    public string? DocumentPath { get; }

    /// <summary>
    /// The operation that was being performed when the error occurred
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Additional context about the error
    /// </summary>
    public string? Context { get; }

    /// <summary>
    /// Initializes a new instance of the DocumentProcessingException class
    /// </summary>
    public DocumentProcessingException() : base("Document processing error")
    {
    }

    /// <summary>
    /// Initializes a new instance of the DocumentProcessingException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DocumentProcessingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DocumentProcessingException class with document and operation details
    /// </summary>
    /// <param name="documentPath">Path to the document that failed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="message">Error message</param>
    public DocumentProcessingException(string documentPath, string operation, string message) 
        : base($"Failed to {operation} document '{Path.GetFileName(documentPath)}': {message}")
    {
        DocumentPath = documentPath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the DocumentProcessingException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DocumentProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DocumentProcessingException class with full details
    /// </summary>
    /// <param name="documentPath">Path to the document that failed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DocumentProcessingException(string documentPath, string operation, string message, Exception innerException) 
        : base($"Failed to {operation} document '{Path.GetFileName(documentPath)}': {message}", innerException)
    {
        DocumentPath = documentPath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance with context information
    /// </summary>
    /// <param name="documentPath">Path to the document that failed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="message">Error message</param>
    /// <param name="context">Additional context</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DocumentProcessingException(string documentPath, string operation, string message, string context, Exception innerException) 
        : base($"Failed to {operation} document '{Path.GetFileName(documentPath)}': {message} (Context: {context})", innerException)
    {
        DocumentPath = documentPath;
        Operation = operation;
        Context = context;
    }

    /// <summary>
    /// Creates a DocumentProcessingException for a file that cannot be opened
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException CannotOpen(string documentPath, Exception innerException)
    {
        return new DocumentProcessingException(documentPath, "open", "Document cannot be opened or is corrupted", innerException);
    }

    /// <summary>
    /// Creates a DocumentProcessingException for a file that cannot be read
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException CannotRead(string documentPath, Exception innerException)
    {
        return new DocumentProcessingException(documentPath, "read", "Document content cannot be read", innerException);
    }

    /// <summary>
    /// Creates a DocumentProcessingException for a file that cannot be written
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException CannotWrite(string documentPath, Exception innerException)
    {
        return new DocumentProcessingException(documentPath, "write", "Document cannot be saved", innerException);
    }

    /// <summary>
    /// Creates a DocumentProcessingException for invalid document structure
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="structureIssue">Description of the structure issue</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException InvalidStructure(string documentPath, string structureIssue, Exception? innerException = null)
    {
        var message = $"Document has invalid structure: {structureIssue}";
        return innerException != null 
            ? new DocumentProcessingException(documentPath, "validate", message, innerException)
            : new DocumentProcessingException(documentPath, "validate", message);
    }

    /// <summary>
    /// Creates a DocumentProcessingException for placeholder scanning errors
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="scanningIssue">Description of the scanning issue</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException ScanningFailed(string documentPath, string scanningIssue, Exception innerException)
    {
        return new DocumentProcessingException(documentPath, "scan for placeholders", scanningIssue, innerException);
    }

    /// <summary>
    /// Creates a DocumentProcessingException for placeholder replacement errors
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="replacementIssue">Description of the replacement issue</param>
    /// <param name="context">Additional context about what was being replaced</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>DocumentProcessingException instance</returns>
    public static DocumentProcessingException ReplacementFailed(string documentPath, string replacementIssue, string context, Exception innerException)
    {
        return new DocumentProcessingException(documentPath, "replace placeholders", replacementIssue, context, innerException);
    }
}