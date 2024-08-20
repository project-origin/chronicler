using Xunit;
using Moq;
using ProjectOrigin.Chronicler.Server;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Options;
using System.Collections.Generic;
using System;
using ProjectOrigin.Chronicler.Server.Exceptions;

namespace ProjectOrigin.Chronicler.Test;

public class RegistryClientFactoryTests
{
    private const string RegistryName = "someRegistry";

    [Fact]
    public void GetClient_WhenRegistryNotKnown_ThrowsRegistryNotKnownException()
    {
        // Arrange
        var mockOptionsMonitor = new Mock<IOptionsMonitor<NetworkOptions>>();
        var options = new NetworkOptions
        {
            Registries = new Dictionary<string, RegistryInfo>()
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(options);
        var factory = new RegistryClientFactory(mockOptionsMonitor.Object);

        // Act
        Action act = () => factory.GetClient(RegistryName);

        // Assert
        act.Should().Throw<RegistryNotKnownException>();
    }

    [Fact]
    public void GetClient_WhenRegistryKnown_ReturnsClient()
    {
        // Arrange
        var mockOptionsMonitor = new Mock<IOptionsMonitor<NetworkOptions>>();
        var options = new NetworkOptions
        {
            Registries = new Dictionary<string, RegistryInfo>{
                { RegistryName, new RegistryInfo { Url = "http://example.com" }}
            }
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(options);
        var factory = new RegistryClientFactory(mockOptionsMonitor.Object);

        // Act
        var result = factory.GetClient(RegistryName);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GetChannel_WhenCalledMultipleTimes_ReturnsSameInstance()
    {
        // Arrange
        var mockOptionsMonitor = new Mock<IOptionsMonitor<NetworkOptions>>();
        var options = new NetworkOptions
        {
            Registries = new Dictionary<string, RegistryInfo>{
                { RegistryName, new RegistryInfo { Url = "http://example.com" }}
            }
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(options);
        var factory = new RegistryClientFactory(mockOptionsMonitor.Object);

        // Act
        var result1 = factory.GetChannel(RegistryName);
        var result2 = factory.GetChannel(RegistryName);

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void GetChannel_NewInstance_IfMonitorChanged()
    {
        // Arrange
        Action<NetworkOptions, string?>? onChange = null;
        var mockOptionsMonitor = new Mock<IOptionsMonitor<NetworkOptions>>();
        mockOptionsMonitor.Setup(m => m.OnChange(It.IsAny<Action<NetworkOptions, string?>>())).Callback<Action<NetworkOptions, string?>>((action) => onChange = action);

        var options = new NetworkOptions
        {
            Registries = new Dictionary<string, RegistryInfo>{
                { RegistryName, new RegistryInfo { Url = "http://example.com" }}
            }
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(options);
        var factory = new RegistryClientFactory(mockOptionsMonitor.Object);

        // Act
        var result1 = factory.GetChannel(RegistryName);
        var options2 = new NetworkOptions
        {
            Registries = new Dictionary<string, RegistryInfo>{
                { RegistryName, new RegistryInfo { Url = "http://example.com" }}
            }
        };
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(options);
        onChange?.Invoke(options2, null);
        var result2 = factory.GetChannel(RegistryName);

        // Assert
        Assert.NotSame(result1, result2);
    }
}
