using Xunit.Abstractions;
using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Core.Interceptors;
using Grpc.Core;
using ProjectOrigin.TestUtils.Fixtures;
using ProjectOrigin.TestUtils.Fixtures.ServerAuxiliaries;

namespace ProjectOrigin.TestUtils;

public abstract class TestServerBase<TStartup> : IClassFixture<TestServerFixture<TStartup>>, IDisposable where TStartup : class
{
    protected readonly TestServerFixture<TStartup> _serverFixtur;
    private readonly ExceptionInterceptor _exceptionInterceptor;

    protected CallInvoker Channel => _serverFixtur.Channel.Intercept(_exceptionInterceptor);

    private readonly IDisposable _logger;
    private bool _disposed = false;

    public TestServerBase(TestServerFixture<TStartup> serverFixture, ITestOutputHelper outputHelper)
    {
        _exceptionInterceptor = new ExceptionInterceptor();
        _serverFixtur = serverFixture;
        _logger = serverFixture.SetTestLogger(outputHelper);

        _serverFixtur.ConfigureTestServices += (serviceCollection) =>
        {
            serviceCollection.AddGrpc(options => options.Interceptors.Add<ExceptionInterceptor>());
            serviceCollection.AddSingleton(_exceptionInterceptor);
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _logger.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~TestServerBase()
    {
        Dispose(false);
    }
}
