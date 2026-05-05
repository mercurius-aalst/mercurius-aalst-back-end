namespace Auth.Module.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid username or password.") { }
    public InvalidCredentialsException(string message) : base(message) { }
}

public class LockoutException : Exception
{
    public LockoutException() : base("Account is temporarily locked due to too many failed login attempts.") { }
}
