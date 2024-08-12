using System;

namespace ProjectOrigin.Chronicler.Server.Exceptions
{
    public class RegistryNotKnownException : Exception
    {
        public RegistryNotKnownException(string registryName)
            : base($"Registry {registryName} is not known")
        {
        }
    }
}
