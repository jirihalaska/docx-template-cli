using DocxTemplate.Core.Services;
using DocxTemplate.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocxTemplate.CLI.Tests.Commands;

public class DiscoverCommandTests
{
    [Fact]
    public void ITemplateDiscoveryService_InterfaceExists()
    {
        // arrange & act
        var interfaceType = typeof(ITemplateDiscoveryService);

        // assert
        interfaceType.Should().NotBeNull();
        interfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void CreateMockLogger_ReturnsValidMock()
    {
        // arrange & act
        var mockLogger = MockServiceFactory.CreateMockLogger<string>();

        // assert
        mockLogger.Should().NotBeNull();
        mockLogger.Object.Should().NotBeNull();
    }
}