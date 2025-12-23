using System.Reflection;
using Xunit;

namespace ImaToDicomConverter.Tests;

public class SimpleResourceTest
{
    [Fact]
    public void EmbeddedResource_ShouldExist()
    {
        // Arrange
        var assembly = typeof(ConfigGenerator).Assembly;
        
        // Act
        var resources = assembly.GetManifestResourceNames();
        
        // Assert
        Assert.NotEmpty(resources);
        Assert.Contains("ImaToDicomConverter.default-config.json", resources);
    }
}

