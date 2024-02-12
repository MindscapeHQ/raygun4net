using System;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
    public class WrapperException : Exception
    {
        public WrapperException(Exception innerException)
            : base("Something went wrong", innerException)
        {
        }
    }
}
