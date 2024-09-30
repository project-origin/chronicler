using System;

namespace ProjectOrigin.Chronicler.Exceptions
{
    public class RegistryNotKnownException : Exception
    {
        public RegistryNotKnownException(string registryName)
            : base($"Registry {registryName} is not known")
        {
        }
    }
}
