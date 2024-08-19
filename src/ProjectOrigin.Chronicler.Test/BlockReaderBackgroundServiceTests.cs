using System.Threading.Tasks;
using Xunit;
using ProjectOrigin.Chronicler.Server.BlockReader;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Options;
using System;
using System.Threading;

namespace ProjectOrigin.Chronicler.Test;

public class BlockReaderBackgroundServiceTests
{

    [Fact]
    public async Task VerifyBackgroundService_CallsIBlockReaderJob_JobInterval()
    {
        // Arrange
        var job = new Mock<IBlockReaderJob>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(provider => provider.GetService(typeof(IBlockReaderJob)))
                   .Returns(job.Object);
        var service = new BlockReaderBackgroundService(
            Mock.Of<ILogger<BlockReaderBackgroundService>>(),
            Options.Create(new ChroniclerOptions
            {
                SigningKeyFilename = "",
                GridAreas = [],
                JobInterval = TimeSpan.FromMilliseconds(100)
            }),
            serviceProvider.Object);

        // Act
        await service.StartAsync(default);

        // Assert
        await Task.Delay(1000);
        job.Verify(x => x.ProcessAllRegistries(It.IsAny<CancellationToken>()), Times.AtLeast(8));
        job.Verify(x => x.ProcessAllRegistries(It.IsAny<CancellationToken>()), Times.AtMost(14));
    }

    [Fact]
    public async Task VerifyBackgroundService_FaultsWhenJobsFailsWithException()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<BlockReaderBackgroundService>>();

        serviceProvider.Setup(provider => provider.GetService(typeof(IBlockReaderJob)))
                   .Returns(new FakeJob());
        var service = new BlockReaderBackgroundService(
            logger.Object,
            Options.Create(new ChroniclerOptions
            {
                SigningKeyFilename = "",
                GridAreas = [],
                JobInterval = TimeSpan.FromSeconds(1)
            }),
            serviceProvider.Object);

        // Act
        await service.StartAsync(default);
        await Task.Delay(500);

        // Assert
        logger.Verify(x => x.Log(LogLevel.Trace, 0, It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        logger.Verify(x => x.Log(LogLevel.Critical, 0, It.IsAny<It.IsAnyType>(), It.IsAny<TestException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        logger.VerifyNoOtherCalls();
    }

    private class TestException : Exception
    {
    }

    private class FakeJob : IBlockReaderJob
    {
        public Task ProcessAllRegistries(CancellationToken cancellationToken)
        {
            throw new TestException();
        }

        public Task ProcessRegistryBlocks(string registryName, int previousBlockHeight, CancellationToken cancellationToken)
        {
            throw new TestException();
        }
    }
}
