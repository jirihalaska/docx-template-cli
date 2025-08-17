using System.Text;

namespace DocxTemplate.Infrastructure.Services;

public interface IFileSystemService
{
    bool FileExists(string filePath);
    bool DirectoryExists(string directoryPath);
    
    Task<string> ReadAllTextAsync(string filePath, Encoding? encoding = null, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string filePath, string contents, Encoding? encoding = null, CancellationToken cancellationToken = default);
    Task WriteAllBytesAsync(string filePath, byte[] bytes, CancellationToken cancellationToken = default);
    
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    
    void CreateDirectory(string path);
    void DeleteFile(string filePath);
    void DeleteDirectory(string path, bool recursive = false);
    
    void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);
    void MoveFile(string sourceFileName, string destFileName);
    
    string GetFileName(string path);
    string GetFileNameWithoutExtension(string path);
    string GetDirectoryName(string path);
    string GetExtension(string path);
    string GetFullPath(string path);
    string Combine(params string[] paths);
    
    long GetFileSize(string filePath);
    DateTime GetLastWriteTime(string path);
    
    Task<string> CreateBackupAsync(string filePath, CancellationToken cancellationToken = default);
    Task RestoreFromBackupAsync(string backupPath, string originalPath, CancellationToken cancellationToken = default);
    void DeleteBackup(string backupPath);
}