using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

public class CliExecutableDiscoveryServiceTests
{
    private readonly CliExecutableDiscoveryService _service = new();

    [Fact]
    public async Task DiscoverCliExecutableAsync_WhenExecutableNotFound_ThrowsCliExecutableNotFoundException()
    {
        // arrange
        // act & assert
        var exception = await Assert.ThrowsAsync<CliExecutableNotFoundException>(
            () => _service.DiscoverCliExecutableAsync());
        
        Assert.Contains("CLI executable not found", exception.Message);
        Assert.NotNull(exception.SearchDirectory);
    }

    [Fact]
    public async Task ValidateCliExecutableAsync_WithNonExistentFile_ReturnsFalse()
    {
        // arrange
        var nonExistentPath = "/path/that/does/not/exist/docx-template";
        
        // act
        var result = await _service.ValidateCliExecutableAsync(nonExistentPath);
        
        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCliExecutableAsync_WithInvalidExecutable_ReturnsFalse()
    {
        // arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "This is not an executable");
            
            // act
            var result = await _service.ValidateCliExecutableAsync(tempFile);
            
            // assert
            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Constructor_CreatesService_Successfully()
    {
        // arrange & act
        var service = new CliExecutableDiscoveryService();
        
        // assert
        Assert.NotNull(service);
    }
}