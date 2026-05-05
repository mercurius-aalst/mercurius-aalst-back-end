using Mercurius.Shared.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
    public User Captain { get; set; }

    public IList<User> Members { get; set; } = new List<User>();
    public IList<TeamInvite> TeamInvites { get; set; } = new List<TeamInvite>();

    public Team()
    {

    }

    public Team(string name, User captain)
    {
        Name = name;
        Captain = captain;
        CaptainUserId = captain.Id;
        Members.Add(captain);
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void ChangeCaptain(Guid captainUserId)
    {
        if (!Members.Any(m => m.Id == captainUserId))
            throw new ValidationException($"New captain must be part of the team.");

        CaptainUserId = captainUserId;
    }

    public void RemoveMember(Guid userId)
    {
        var member = Members.FirstOrDefault(m => m.Id == userId);
        if (member is null)
            throw new NotFoundException($"{nameof(User)} not found in {Name}");
        if (member.Id == CaptainUserId)
            throw new ValidationException("The captain cannot be removed from a team");
        Members.Remove(member);
    }

    public TeamInvite InviteUser(Guid userId, int inviteResendCooldownDays)
    {
        if (Members.Any(p => p.Id == userId))
            throw new ValidationException("User is already in the team");
        if (TeamInvites.Any(i => i.UserId == userId && i.Status == TeamInviteStatus.Pending))
            throw new ValidationException("User already has a pending invite to this team");
        var lastDeclinedInvite = TeamInvites
           .Where(i => i.UserId == userId && i.Status == TeamInviteStatus.Declined)
           .OrderByDescending(i => i.RespondedAt)
           .FirstOrDefault();
        if (lastDeclinedInvite != null && lastDeclinedInvite.RespondedAt.HasValue)
        {
            var daysSinceDeclined = (DateTime.UtcNow - lastDeclinedInvite.RespondedAt.Value).TotalDays;
            if (daysSinceDeclined < inviteResendCooldownDays)
            {
                throw new ValidationException($"User declined the last invite less than {inviteResendCooldownDays} days ago. Please wait {inviteResendCooldownDays - (int)daysSinceDeclined} more day(s).");
            }
        }
        var invite = new TeamInvite { TeamId = Id, UserId = userId };
        TeamInvites.Add(invite);
        return invite;
    }
}

