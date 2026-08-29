namespace Mercurius.Modules.Shared.Exceptions;

public sealed class ForbiddenException : Exception
{
    public string Code { get; }

    public ForbiddenException(string code, string message) : base(message)
    {
        Code = code;
    }
}
