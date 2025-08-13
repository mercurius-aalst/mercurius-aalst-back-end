using System;

namespace MercuriusAPI.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid username or password.") { }
        public InvalidCredentialsException(string message) : base(message) { }
    }

    public class LockoutException : Exception
    {
        public LockoutException() : base("Account is temporarily locked due to too many failed login attempts.") { }
    }
}
