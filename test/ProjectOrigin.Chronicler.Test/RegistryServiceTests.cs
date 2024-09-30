using System.Threading.Tasks;
using Xunit;
using Moq;
using Grpc.Core;
using FluentAssertions;

namespace ProjectOrigin.Chronicler.Test;

public class RegistryServiceTests
{
    private const string RegistryName = "someRegistry";
    private const string GridArea = "Narnia";
    private readonly Mock<IRegistryClientFactory> _factory;

    public RegistryServiceTests()
    {
        _factory = new Mock<IRegistryClientFactory>();
    }

    [Fact]
    public async Task GetNextBlock_EmptyResponse_ReturnsNull()
    {
        // Arrange
        var registryService = new RegistryService(_factory.Object);
        var mockClient = new Mock<Registry.V1.RegistryService.RegistryServiceClient>();
        mockClient.Setup(x => x.GetBlocksAsync(It.IsAny<Registry.V1.GetBlocksRequest>(), null, null, default))
            .Returns(CreateAsyncUnaryCall(new Registry.V1.GetBlocksResponse
            {
                Blocks = { }
            }));
        _factory.Setup(x => x.GetClient(RegistryName)).Returns(mockClient.Object);

        // Act
        var result = await registryService.GetNextBlock(RegistryName, 0);

        // Assert
        _factory.Verify(x => x.GetClient(RegistryName), Times.Once);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(8)]
    public async Task GetNextBlock_ContainsSingle_ReturnsSingle(int height)
    {
        // Arrange
        var registryService = new RegistryService(_factory.Object);
        var mockClient = new Mock<Registry.V1.RegistryService.RegistryServiceClient>();
        mockClient.Setup(x => x.GetBlocksAsync(It.IsAny<Registry.V1.GetBlocksRequest>(), null, null, default))
            .Returns(CreateAsyncUnaryCall(new Registry.V1.GetBlocksResponse
            {
                Blocks = {
                    new Registry.V1.Block { Height = height+1 }
                }
            }));
        _factory.Setup(x => x.GetClient(RegistryName)).Returns(mockClient.Object);

        // Act
        var result = await registryService.GetNextBlock(RegistryName, height);

        // Assert
        _factory.Verify(x => x.GetClient(RegistryName), Times.Once);
        mockClient.Verify(x => x.GetBlocksAsync(It.Is<Registry.V1.GetBlocksRequest>(
            x => x.Skip == height
            && x.Limit == 1
            && x.IncludeTransactions == true
            ), null, null, default), Times.Once);
        mockClient.VerifyNoOtherCalls();
        result.Should().NotBeNull();
        result!.Height.Should().Be(height + 1);
    }

    [Fact]
    public async Task GetNextBlock_ContainsMultiple_RaiseError()
    {
        // Arrange
        var registryService = new RegistryService(_factory.Object);
        var mockClient = new Mock<Registry.V1.RegistryService.RegistryServiceClient>();
        mockClient.Setup(x => x.GetBlocksAsync(It.IsAny<Registry.V1.GetBlocksRequest>(), null, null, default))
            .Returns(CreateAsyncUnaryCall(new Registry.V1.GetBlocksResponse
            {
                Blocks = {
                    new Registry.V1.Block { Height = 0 },
                    new Registry.V1.Block { Height = 1 }
                }
            }));
        _factory.Setup(x => x.GetClient(RegistryName)).Returns(mockClient.Object);

        // Assert
        await Assert.ThrowsAsync<System.InvalidOperationException>(() => registryService.GetNextBlock(RegistryName, 0));
    }

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

}
