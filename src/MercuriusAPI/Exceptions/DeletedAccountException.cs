namespace Mercurius.LAN.API.Exceptions;

public sealed class DeletedAccountException : Exception
{
    public DeletedAccountException() : base("This account has been deleted.")
    {
    }
}
