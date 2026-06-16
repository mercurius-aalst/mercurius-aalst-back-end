namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public enum RosterSelectionAction
{
    Confirm,
    Decline
}

public record RosterSelectionActionDTO(RosterSelectionAction Action);
