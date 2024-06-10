using System;

namespace ProjectOrigin.TestUtils;


[Serializable]
public class GrpcServerException : Exception
{
    public GrpcServerException(Exception? innerException) :
        base("Exception thrown by gRPC server, with inner exception", innerException)
    {
    }
}
