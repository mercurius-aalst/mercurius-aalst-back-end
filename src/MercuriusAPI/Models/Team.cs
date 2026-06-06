using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public Guid CaptainUserId { get; set; }
    public User Captain { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public IList<User> Members { get; set; } = new List<User>();
    public IList<TeamInvite> TeamInvites { get; set; } = new List<TeamInvite>();

    public Team()
    {

    }

    public Team(string name, User captain)
    {
        UpdateName(name);
        Captain = captain;
        CaptainUserId = captain.Id;
        Members.Add(captain);
    }

    public void UpdateName(string name)
    {
        var displayName = NormalizeDisplayName(name);
        Name = displayName;
        NormalizedName = displayName.ToLowerInvariant();
    }

    public static string NormalizeName(string name)
    {
        return NormalizeDisplayName(name).ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Team name is required.");

        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
            throw new ValidationException("Team name cannot exceed 100 characters.");
        if (trimmedName.Any(char.IsControl))
            throw new ValidationException("Team name cannot contain control characters.");

        return trimmedName;
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

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
    }

    public TeamInvite InviteUser(Guid userId, int inviteResendCooldownDays, int inviteExpirationDays = 14, int declinedInviteResendLimit = 3)
    {
        if (Members.Any(p => p.Id == userId))
            throw new ValidationException("User is already in the team");
        if (TeamInvites.Any(i => i.UserId == userId && i.Status == TeamInviteStatus.Pending))
            throw new ValidationException("User already has a pending invite to this team");
        var cooldownStart = DateTime.UtcNow.AddDays(-inviteResendCooldownDays);
        var declinedInvitesInCooldown = TeamInvites
            .Where(i =>
                i.UserId == userId &&
                i.Status == TeamInviteStatus.Declined &&
                i.RespondedAt.HasValue &&
                i.RespondedAt.Value >= cooldownStart)
            .OrderByDescending(i => i.RespondedAt)
            .ToList();

        if (declinedInvitesInCooldown.Count >= declinedInviteResendLimit)
        {
            var lastDeclinedInvite = declinedInvitesInCooldown[0];
            var daysSinceDeclined = (DateTime.UtcNow - lastDeclinedInvite.RespondedAt!.Value).TotalDays;
            if (daysSinceDeclined < inviteResendCooldownDays)
            {
                throw new ValidationException($"User declined {declinedInviteResendLimit} invites in the cooldown window. Please wait {inviteResendCooldownDays - (int)daysSinceDeclined} more day(s).");
            }
        }
        var now = DateTime.UtcNow;
        var invite = new TeamInvite
        {
            TeamId = Id,
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(inviteExpirationDays)
        };
        TeamInvites.Add(invite);
        return invite;
    }
}

