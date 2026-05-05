namespace Mercurius.LAN.API.DTOs.Auth;

public class CurrentUserProfileResponse
{
    public bool IsComplete { get; set; }
    public GetUserDTO? User { get; set; }

    public CurrentUserProfileResponse(bool isComplete, GetUserDTO? user)
    {
        IsComplete = isComplete;
        User = user;
    }
}
