namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Exception thrown when file I/O operations fail
/// </summary>
public class FileAccessException : DocxTemplateException
{
    /// <summary>
    /// Path to the file that could not be accessed
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// The operation that was being performed when the error occurred
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Type of file access that was attempted
    /// </summary>
    public FileAccessType AccessType { get; }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class
    /// </summary>
    public FileAccessException() : base("File access error")
    {
        AccessType = FileAccessType.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public FileAccessException(string message) : base(message)
    {
        AccessType = FileAccessType.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class with file and operation details
    /// </summary>
    /// <param name="filePath">Path to the file that could not be accessed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="accessType">Type of access that was attempted</param>
    public FileAccessException(string filePath, string operation, FileAccessType accessType) 
        : base($"Failed to {operation} file '{Path.GetFileName(filePath)}': {GetAccessTypeDescription(accessType)} access denied")
    {
        FilePath = filePath;
        Operation = operation;
        AccessType = accessType;
    }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class with full details
    /// </summary>
    /// <param name="filePath">Path to the file that could not be accessed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="accessType">Type of access that was attempted</param>
    /// <param name="message">Detailed error message</param>
    public FileAccessException(string filePath, string operation, FileAccessType accessType, string message) 
        : base($"Failed to {operation} file '{Path.GetFileName(filePath)}': {message}")
    {
        FilePath = filePath;
        Operation = operation;
        AccessType = accessType;
    }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public FileAccessException(string message, Exception innerException) : base(message, innerException)
    {
        AccessType = FileAccessType.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the FileAccessException class with full details and inner exception
    /// </summary>
    /// <param name="filePath">Path to the file that could not be accessed</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="accessType">Type of access that was attempted</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public FileAccessException(string filePath, string operation, FileAccessType accessType, Exception innerException) 
        : base($"Failed to {operation} file '{Path.GetFileName(filePath)}': {innerException.Message}", innerException)
    {
        FilePath = filePath;
        Operation = operation;
        AccessType = accessType;
    }

    /// <summary>
    /// Creates a FileAccessException for a file that cannot be read
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException CannotRead(string filePath, Exception innerException)
    {
        return new FileAccessException(filePath, "read", FileAccessType.Read, innerException);
    }

    /// <summary>
    /// Creates a FileAccessException for a file that cannot be written
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException CannotWrite(string filePath, Exception innerException)
    {
        return new FileAccessException(filePath, "write", FileAccessType.Write, innerException);
    }

    /// <summary>
    /// Creates a FileAccessException for a file that cannot be created
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException CannotCreate(string filePath, Exception innerException)
    {
        return new FileAccessException(filePath, "create", FileAccessType.Create, innerException);
    }

    /// <summary>
    /// Creates a FileAccessException for a file that cannot be deleted
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException CannotDelete(string filePath, Exception innerException)
    {
        return new FileAccessException(filePath, "delete", FileAccessType.Delete, innerException);
    }

    /// <summary>
    /// Creates a FileAccessException for a directory that cannot be created
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <param name="innerException">The underlying exception</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException CannotCreateDirectory(string directoryPath, Exception innerException)
    {
        return new FileAccessException(directoryPath, "create directory", FileAccessType.Create, innerException);
    }

    /// <summary>
    /// Creates a FileAccessException for insufficient permissions
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="requiredAccess">Type of access that was required</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException InsufficientPermissions(string filePath, FileAccessType requiredAccess)
    {
        return new FileAccessException(filePath, "access", requiredAccess, "Insufficient permissions");
    }

    /// <summary>
    /// Creates a FileAccessException for a file that is locked or in use
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="operation">Operation that was attempted</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException FileInUse(string filePath, string operation)
    {
        return new FileAccessException(filePath, operation, FileAccessType.Write, "File is locked or in use by another process");
    }

    /// <summary>
    /// Creates a FileAccessException for insufficient disk space
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="operation">Operation that was attempted</param>
    /// <returns>FileAccessException instance</returns>
    public static FileAccessException InsufficientDiskSpace(string filePath, string operation)
    {
        return new FileAccessException(filePath, operation, FileAccessType.Write, "Insufficient disk space");
    }

    private static string GetAccessTypeDescription(FileAccessType accessType)
    {
        return accessType switch
        {
            FileAccessType.Read => "Read",
            FileAccessType.Write => "Write",
            FileAccessType.Create => "Create",
            FileAccessType.Delete => "Delete",
            FileAccessType.ReadWrite => "Read/Write",
            _ => "File"
        };
    }
}

/// <summary>
/// Enumeration of file access types
/// </summary>
public enum FileAccessType
{
    /// <summary>
    /// Unknown or unspecified access type
    /// </summary>
    Unknown,

    /// <summary>
    /// Read access
    /// </summary>
    Read,

    /// <summary>
    /// Write access
    /// </summary>
    Write,

    /// <summary>
    /// Create access (creating new files)
    /// </summary>
    Create,

    /// <summary>
    /// Delete access
    /// </summary>
    Delete,

    /// <summary>
    /// Both read and write access
    /// </summary>
    ReadWrite,
}