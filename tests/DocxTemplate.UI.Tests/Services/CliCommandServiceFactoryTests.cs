using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

public class CliCommandServiceFactoryTests
{
    [Fact]
    public async Task CreateAsync_WithValidDiscovery_ReturnsCliCommandService()
    {
        // arrange
        var mockDiscovery = new Mock<ICliExecutableDiscoveryService>();
        mockDiscovery.Setup(x => x.DiscoverCliExecutableAsync())
            .ReturnsAsync("/path/to/cli.exe");
        
        var factory = new CliCommandServiceFactory(mockDiscovery.Object);
        
        // act
        var service = await factory.CreateAsync();
        
        // assert
        Assert.NotNull(service);
        Assert.IsType<CliProcessRunner>(service);
    }

    [Fact]
    public async Task CreateAsync_WhenDiscoveryThrows_PropagatesException()
    {
        // arrange
        var mockDiscovery = new Mock<ICliExecutableDiscoveryService>();
        mockDiscovery.Setup(x => x.DiscoverCliExecutableAsync())
            .ThrowsAsync(new CliExecutableNotFoundException("/some/path"));
        
        var factory = new CliCommandServiceFactory(mockDiscovery.Object);
        
        // act & assert
        await Assert.ThrowsAsync<CliExecutableNotFoundException>(
            () => factory.CreateAsync());
    }
}