using System.Text;
using DocxTemplate.Core.Exceptions;

namespace DocxTemplate.Infrastructure.Services;

public class FileSystemService : IFileSystemService
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public bool FileExists(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return File.Exists(filePath);
    }

    public bool DirectoryExists(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        return Directory.Exists(directoryPath);
    }

    public async Task<string> ReadAllTextAsync(string filePath, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        try
        {
            if (!FileExists(filePath))
            {
                throw new FileAccessException($"File not found: {filePath}");
            }

            return await File.ReadAllTextAsync(filePath, encoding ?? DefaultEncoding, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied reading file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error reading file: {filePath}", ex);
        }
    }

    public async Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        try
        {
            if (!FileExists(filePath))
            {
                throw new FileAccessException($"File not found: {filePath}");
            }

            return await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied reading file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error reading file: {filePath}", ex);
        }
    }

    public async Task WriteAllTextAsync(string filePath, string contents, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(contents);

        try
        {
            var directory = GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, contents, encoding ?? DefaultEncoding, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied writing file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error writing file: {filePath}", ex);
        }
    }

    public async Task WriteAllBytesAsync(string filePath, byte[] bytes, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            var directory = GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied writing file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error writing file: {filePath}", ex);
        }
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        try
        {
            if (!DirectoryExists(path))
            {
                throw new FileAccessException($"Directory not found: {path}");
            }

            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied enumerating files in: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error enumerating files in: {path}", ex);
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        try
        {
            if (!DirectoryExists(path))
            {
                throw new FileAccessException($"Directory not found: {path}");
            }

            return Directory.EnumerateDirectories(path, searchPattern, searchOption);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied enumerating directories in: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error enumerating directories in: {path}", ex);
        }
    }

    public void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            Directory.CreateDirectory(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied creating directory: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error creating directory: {path}", ex);
        }
    }

    public void DeleteFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (FileExists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied deleting file: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error deleting file: {filePath}", ex);
        }
    }

    public void DeleteDirectory(string path, bool recursive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (DirectoryExists(path))
            {
                Directory.Delete(path, recursive);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied deleting directory: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error deleting directory: {path}", ex);
        }
    }

    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destFileName);

        try
        {
            if (!FileExists(sourceFileName))
            {
                throw new FileAccessException($"Source file not found: {sourceFileName}");
            }

            var destDirectory = GetDirectoryName(destFileName);
            if (!string.IsNullOrEmpty(destDirectory) && !DirectoryExists(destDirectory))
            {
                CreateDirectory(destDirectory);
            }

            File.Copy(sourceFileName, destFileName, overwrite);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied copying file from {sourceFileName} to {destFileName}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error copying file from {sourceFileName} to {destFileName}", ex);
        }
    }

    public void MoveFile(string sourceFileName, string destFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destFileName);

        try
        {
            if (!FileExists(sourceFileName))
            {
                throw new FileAccessException($"Source file not found: {sourceFileName}");
            }

            var destDirectory = GetDirectoryName(destFileName);
            if (!string.IsNullOrEmpty(destDirectory) && !DirectoryExists(destDirectory))
            {
                CreateDirectory(destDirectory);
            }

            File.Move(sourceFileName, destFileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied moving file from {sourceFileName} to {destFileName}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error moving file from {sourceFileName} to {destFileName}", ex);
        }
    }

    public string GetFileName(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFileName(path);
    }

    public string GetFileNameWithoutExtension(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFileNameWithoutExtension(path);
    }

    public string GetDirectoryName(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetDirectoryName(path) ?? string.Empty;
    }

    public string GetExtension(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetExtension(path);
    }

    public string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    public string Combine(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Length == 0)
        {
            throw new ArgumentException("At least one path must be provided", nameof(paths));
        }

        return Path.Combine(paths);
    }

    public long GetFileSize(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!FileExists(filePath))
            {
                throw new FileAccessException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied accessing file info: {filePath}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error accessing file info: {filePath}", ex);
        }
    }

    public DateTime GetLastWriteTime(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (!FileExists(path))
            {
                throw new FileAccessException($"File not found: {path}");
            }

            return File.GetLastWriteTime(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied accessing file time: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new FileAccessException($"I/O error accessing file time: {path}", ex);
        }
    }

    public async Task<string> CreateBackupAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!FileExists(filePath))
        {
            throw new FileAccessException($"Cannot create backup: source file not found: {filePath}");
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var extension = GetExtension(filePath);
        var nameWithoutExtension = GetFileNameWithoutExtension(filePath);
        var directory = GetDirectoryName(filePath);
        
        var backupFileName = $"{nameWithoutExtension}.backup.{timestamp}{extension}";
        var backupPath = Combine(directory, backupFileName);

        try
        {
            var sourceBytes = await ReadAllBytesAsync(filePath, cancellationToken);
            await WriteAllBytesAsync(backupPath, sourceBytes, cancellationToken);
            return backupPath;
        }
        catch (Exception ex)
        {
            throw new FileAccessException($"Failed to create backup of {filePath}", ex);
        }
    }

    public async Task RestoreFromBackupAsync(string backupPath, string originalPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);

        if (!FileExists(backupPath))
        {
            throw new FileAccessException($"Cannot restore: backup file not found: {backupPath}");
        }

        try
        {
            var backupBytes = await ReadAllBytesAsync(backupPath, cancellationToken);
            await WriteAllBytesAsync(originalPath, backupBytes, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new FileAccessException($"Failed to restore from backup {backupPath} to {originalPath}", ex);
        }
    }

    public void DeleteBackup(string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        
        try
        {
            DeleteFile(backupPath);
        }
        catch (Exception ex)
        {
            throw new FileAccessException($"Failed to delete backup: {backupPath}", ex);
        }
    }
}