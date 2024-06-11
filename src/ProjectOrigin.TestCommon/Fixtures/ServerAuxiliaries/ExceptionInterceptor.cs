using System;
using Grpc.Core.Interceptors;
using System.Threading.Tasks;
using Grpc.Core;

namespace ProjectOrigin.TestCommon.Fixtures.ServerAuxiliaries;

internal class ExceptionInterceptor : Interceptor
{
    private Exception? _exception = null;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
           TRequest request,
           ClientInterceptorContext<TRequest, TResponse> context,
           AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponse(call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> inner)
    {
        try
        {
            return await inner;
        }
        catch (RpcException ex)
        {

            if (ex.StatusCode == StatusCode.Unknown)
                throw new GrpcServerException(_exception);

            throw;
        }
        finally
        {
            _exception = null;
        }
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            _exception = null;
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            _exception = ex;
            throw;
        }
    }
}
