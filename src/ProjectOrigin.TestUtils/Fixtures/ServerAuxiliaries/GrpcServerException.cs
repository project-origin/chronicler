using System;

namespace ProjectOrigin.TestUtils.Fixtures.ServerAuxiliaries;

public class GrpcServerException : Exception
{
    public GrpcServerException(Exception? innerException) :
        base("Exception thrown by gRPC server, with inner exception", innerException)
    {
    }
}
