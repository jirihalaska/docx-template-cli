using Microsoft.Extensions.Logging;
using Moq;

namespace DocxTemplate.TestUtilities;

/// <summary>
/// Factory for creating mock services for testing
/// </summary>
public static class MockServiceFactory
{
    /// <summary>
    /// Creates a mock ILogger for testing
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}