namespace Mercurius.Modules.Teams.Contracts;

public static class TeamRealtimeGroups
{
    public static string GetTeamGroup(Guid teamId) => $"team:{teamId:N}";

    public static string GetUserGroup(Guid userId) => $"user:{userId:N}";
}
