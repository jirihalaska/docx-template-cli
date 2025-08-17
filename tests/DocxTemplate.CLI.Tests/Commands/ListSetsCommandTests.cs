using DocxTemplate.Core.Services;
using FluentAssertions;

namespace DocxTemplate.CLI.Tests.Commands;

public class ListSetsCommandTests
{
    [Fact]
    public void ITemplateSetService_InterfaceExists()
    {
        // arrange & act
        var interfaceType = typeof(ITemplateSetService);

        // assert
        interfaceType.Should().NotBeNull();
        interfaceType.IsInterface.Should().BeTrue();
    }
}