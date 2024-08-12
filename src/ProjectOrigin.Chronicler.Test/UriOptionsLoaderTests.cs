using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using ProjectOrigin.ServiceCommon.UriOptionsLoader;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using System;

namespace ProjectOrigin.Chronicler.Test;

public class UriOptionsLoaderTests
{
    private const string TestPath = "/TestPath";
    private const string Scenario = "MyScenario";
    private const string SecondCallState = "Second Call";

    [Fact]
    public async Task WhenChanged_MonitorCalled()
    {
        var _networkMockServer = WireMockServer.Start();

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { "TestPath:ConfigurationUri",  _networkMockServer.Urls[0] + TestPath },
                    { "TestPath:RefreshInterval", "00:00:10" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>("TestPath");
            services.AddSingleton<NetworkOptionsChangeListener>();
        });

        _networkMockServer.Given(Request.Create().WithPath(TestPath).UsingGet())
            .InScenario(Scenario)
            .WillSetStateTo(SecondCallState)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeValue",
                }));

        _networkMockServer.Given(Request.Create().WithPath(TestPath).UsingGet())
            .InScenario(Scenario)
            .WhenStateIs(SecondCallState)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeOtherValue",
                }));


        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener>();

        TestOption currentOption = listener.MonitorOption.CurrentValue;
        TestOption? newOption = null;
        var taskCompletionSource = new TaskCompletionSource<bool>();

        listener.MonitorOption.OnChange((options, name) =>
        {
            newOption = options;
            taskCompletionSource.SetResult(true);
        });

        await taskCompletionSource.Task;

        Assert.NotEqual(currentOption, newOption);
    }

    [Fact]
    public async Task WhenNotChanged_MonitorNotCalled()
    {
        var _networkMockServer = WireMockServer.Start();

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { "TestPath:ConfigurationUri",  _networkMockServer.Urls[0] + TestPath },
                    { "TestPath:RefreshInterval", "00:00:5" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>("TestPath");
            services.AddSingleton<NetworkOptionsChangeListener>();
        });

        _networkMockServer.Given(Request.Create().WithPath(TestPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeValue",
                }));

        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener>();
        bool called = false;

        listener.MonitorOption.OnChange((options, name) =>
        {
            called = true;
        });

        await Task.Delay(13000);
        _networkMockServer.LogEntries.Should().HaveCount(3);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task NotSupportedFormat_ThrowsException()
    {
        // Arrange
        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { "TestPath:ConfigurationUri", "some://hello.com" },
                    { "TestPath:RefreshInterval", "00:00:5" },
                }
            }));


        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>("TestPath");
        });

        using var host = _builder.Build();

        // Act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => Task.Run(host.Start));

        // Assert
        exception.Message.Should().Be("Unsupported URI scheme: some");
    }

    internal class NetworkOptionsChangeListener
    {
        public IOptionsMonitor<TestOption> MonitorOption { get; }

        public NetworkOptionsChangeListener(IOptionsMonitor<TestOption> optionsMonitor)
        {
            MonitorOption = optionsMonitor;
        }
    }

    internal record TestOption
    {
        public required string SomeKey { get; init; }
    }
}
