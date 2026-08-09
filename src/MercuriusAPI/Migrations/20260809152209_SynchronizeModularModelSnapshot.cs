using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations;

/// <summary>
/// Synchronizes the EF model snapshot after the hand-authored modular-monolith migrations.
/// </summary>
public partial class SynchronizeModularModelSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE teams.teams ALTER COLUMN "Version" DROP DEFAULT;

            ALTER TABLE competition.games RENAME CONSTRAINT "PK_Games" TO "PK_games";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Games_GameId" TO "FK_matches_games_GameId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Matches_LoserNextMatchId" TO "FK_matches_matches_LoserNextMatchId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Matches_WinnerNextMatchId" TO "FK_matches_matches_WinnerNextMatchId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Teams_TeamLoserId" TO "FK_matches_teams_TeamLoserId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Teams_TeamParticipant1Id" TO "FK_matches_teams_TeamParticipant1Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Teams_TeamParticipant2Id" TO "FK_matches_teams_TeamParticipant2Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Teams_TeamWinnerId" TO "FK_matches_teams_TeamWinnerId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Users_UserLoserId" TO "FK_matches_users_UserLoserId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Users_UserParticipant1Id" TO "FK_matches_users_UserParticipant1Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Users_UserParticipant2Id" TO "FK_matches_users_UserParticipant2Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_Matches_Users_UserWinnerId" TO "FK_matches_users_UserWinnerId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "PK_Matches" TO "PK_matches";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "FK_PlacementTeam_Placements_PlacementId" TO "FK_placement_teams_placements_PlacementId";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "FK_PlacementTeam_Teams_TeamId" TO "FK_placement_teams_teams_TeamId";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "PK_PlacementTeam" TO "PK_placement_teams";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "FK_PlacementUser_Placements_PlacementId" TO "FK_placement_users_placements_PlacementId";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "FK_PlacementUser_Users_UserId" TO "FK_placement_users_users_UserId";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "PK_PlacementUser" TO "PK_placement_users";
            ALTER TABLE competition.placements RENAME CONSTRAINT "FK_Placements_Games_GameId" TO "FK_placements_games_GameId";
            ALTER TABLE competition.placements RENAME CONSTRAINT "PK_Placements" TO "PK_placements";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_TournamentRegistrationRosterMembers_Games_GameId" TO "FK_roster_members_games_GameId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_TournamentRegistrationRosterMembers_Teams_TeamId" TO "FK_roster_members_teams_TeamId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_TournamentRegistrationRosterMembers_TournamentRegistrations~" TO "FK_roster_members_tournament_registrations_TournamentRegistrat~";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_TournamentRegistrationRosterMembers_Users_UserId" TO "FK_roster_members_users_UserId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "PK_TournamentRegistrationRosterMembers" TO "PK_roster_members";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_TournamentRegistrations_Games_GameId" TO "FK_tournament_registrations_games_GameId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_TournamentRegistrations_Teams_TeamId" TO "FK_tournament_registrations_teams_TeamId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_TournamentRegistrations_Users_RegisteredByUserId" TO "FK_tournament_registrations_users_RegisteredByUserId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_TournamentRegistrations_Users_UserId" TO "FK_tournament_registrations_users_UserId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "PK_TournamentRegistrations" TO "PK_tournament_registrations";
            ALTER TABLE discovery.search_index_rebuild_documents RENAME CONSTRAINT "FK_search_index_rebuild_documents_search_index_rebuild_jobs_job" TO "FK_search_index_rebuild_documents_search_index_rebuild_jobs_jo~";
            ALTER TABLE identity.users RENAME CONSTRAINT "PK_Users" TO "PK_users";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "FK_GameSponsorPlacements_Games_GameId" TO "FK_game_sponsor_placements_games_GameId";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "FK_GameSponsorPlacements_Sponsors_SponsorId" TO "FK_game_sponsor_placements_sponsors_SponsorId";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "PK_GameSponsorPlacements" TO "PK_game_sponsor_placements";
            ALTER TABLE sponsorship.sponsors RENAME CONSTRAINT "PK_Sponsors" TO "PK_sponsors";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "FK_TeamInvites_Teams_TeamId" TO "FK_team_invites_teams_TeamId";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "FK_TeamInvites_Users_UserId" TO "FK_team_invites_users_UserId";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "PK_TeamInvites" TO "PK_team_invites";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "FK_TeamUser_Teams_TeamId" TO "FK_team_members_teams_TeamId";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "FK_TeamUser_Users_UserId" TO "FK_team_members_users_UserId";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "PK_TeamUser" TO "PK_team_members";
            ALTER TABLE teams.teams RENAME CONSTRAINT "FK_Teams_Users_CaptainUserId" TO "FK_teams_users_CaptainUserId";
            ALTER TABLE teams.teams RENAME CONSTRAINT "PK_Teams" TO "PK_teams";

            ALTER INDEX competition."IX_Matches_GameId" RENAME TO "IX_matches_GameId";
            ALTER INDEX competition."IX_Matches_LoserNextMatchId" RENAME TO "IX_matches_LoserNextMatchId";
            ALTER INDEX competition."IX_Matches_TeamLoserId" RENAME TO "IX_matches_TeamLoserId";
            ALTER INDEX competition."IX_Matches_TeamParticipant1Id" RENAME TO "IX_matches_TeamParticipant1Id";
            ALTER INDEX competition."IX_Matches_TeamParticipant2Id" RENAME TO "IX_matches_TeamParticipant2Id";
            ALTER INDEX competition."IX_Matches_TeamWinnerId" RENAME TO "IX_matches_TeamWinnerId";
            ALTER INDEX competition."IX_Matches_UserLoserId" RENAME TO "IX_matches_UserLoserId";
            ALTER INDEX competition."IX_Matches_UserParticipant1Id" RENAME TO "IX_matches_UserParticipant1Id";
            ALTER INDEX competition."IX_Matches_UserParticipant2Id" RENAME TO "IX_matches_UserParticipant2Id";
            ALTER INDEX competition."IX_Matches_UserWinnerId" RENAME TO "IX_matches_UserWinnerId";
            ALTER INDEX competition."IX_Matches_WinnerNextMatchId" RENAME TO "IX_matches_WinnerNextMatchId";
            ALTER INDEX competition."IX_PlacementTeam_TeamId" RENAME TO "IX_placement_teams_TeamId";
            ALTER INDEX competition."IX_PlacementUser_UserId" RENAME TO "IX_placement_users_UserId";
            ALTER INDEX competition."IX_Placements_GameId" RENAME TO "IX_placements_GameId";
            ALTER INDEX competition."IX_TournamentRegistrationRosterMembers_GameId_TeamId_UserId" RENAME TO "IX_roster_members_GameId_TeamId_UserId";
            ALTER INDEX competition."IX_TournamentRegistrationRosterMembers_TeamId" RENAME TO "IX_roster_members_TeamId";
            ALTER INDEX competition."IX_TournamentRegistrationRosterMembers_TournamentRegistrationI~" RENAME TO "IX_roster_members_TournamentRegistrationId_ConfirmationStatus";
            ALTER INDEX competition."IX_TournamentRegistrationRosterMembers_UserId" RENAME TO "IX_roster_members_UserId";
            ALTER INDEX competition."IX_TournamentRegistrations_GameId_Status_Kind" RENAME TO "IX_tournament_registrations_GameId_Status_Kind";
            ALTER INDEX competition."IX_TournamentRegistrations_RegisteredByUserId" RENAME TO "IX_tournament_registrations_RegisteredByUserId";
            ALTER INDEX competition."IX_TournamentRegistrations_TeamId" RENAME TO "IX_tournament_registrations_TeamId";
            ALTER INDEX competition."IX_TournamentRegistrations_UserId" RENAME TO "IX_tournament_registrations_UserId";
            ALTER INDEX identity."IX_Users_Auth0UserId" RENAME TO "IX_users_Auth0UserId";
            ALTER INDEX identity."IX_Users_Email" RENAME TO "IX_users_Email";
            ALTER INDEX identity."IX_Users_NormalizedUsername" RENAME TO "IX_users_NormalizedUsername";
            ALTER INDEX identity."IX_Users_Username" RENAME TO "IX_users_Username";
            ALTER INDEX sponsorship."IX_GameSponsorPlacements_GameId" RENAME TO "IX_game_sponsor_placements_GameId";
            ALTER INDEX sponsorship."IX_GameSponsorPlacements_SponsorId" RENAME TO "IX_game_sponsor_placements_SponsorId";
            ALTER INDEX teams."IX_TeamInvites_TeamId_Status_ExpiresAt" RENAME TO "IX_team_invites_TeamId_Status_ExpiresAt";
            ALTER INDEX teams."IX_TeamInvites_UserId_Status_ExpiresAt" RENAME TO "IX_team_invites_UserId_Status_ExpiresAt";
            ALTER INDEX teams."IX_TeamUser_UserId" RENAME TO "IX_team_members_UserId";
            ALTER INDEX teams."IX_Teams_CaptainUserId" RENAME TO "IX_teams_CaptainUserId";
            ALTER INDEX teams."IX_Teams_NormalizedName" RENAME TO "IX_teams_NormalizedName";
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER INDEX teams."IX_teams_NormalizedName" RENAME TO "IX_Teams_NormalizedName";
            ALTER INDEX teams."IX_teams_CaptainUserId" RENAME TO "IX_Teams_CaptainUserId";
            ALTER INDEX teams."IX_team_members_UserId" RENAME TO "IX_TeamUser_UserId";
            ALTER INDEX teams."IX_team_invites_UserId_Status_ExpiresAt" RENAME TO "IX_TeamInvites_UserId_Status_ExpiresAt";
            ALTER INDEX teams."IX_team_invites_TeamId_Status_ExpiresAt" RENAME TO "IX_TeamInvites_TeamId_Status_ExpiresAt";
            ALTER INDEX sponsorship."IX_game_sponsor_placements_SponsorId" RENAME TO "IX_GameSponsorPlacements_SponsorId";
            ALTER INDEX sponsorship."IX_game_sponsor_placements_GameId" RENAME TO "IX_GameSponsorPlacements_GameId";
            ALTER INDEX identity."IX_users_Username" RENAME TO "IX_Users_Username";
            ALTER INDEX identity."IX_users_NormalizedUsername" RENAME TO "IX_Users_NormalizedUsername";
            ALTER INDEX identity."IX_users_Email" RENAME TO "IX_Users_Email";
            ALTER INDEX identity."IX_users_Auth0UserId" RENAME TO "IX_Users_Auth0UserId";
            ALTER INDEX competition."IX_tournament_registrations_UserId" RENAME TO "IX_TournamentRegistrations_UserId";
            ALTER INDEX competition."IX_tournament_registrations_TeamId" RENAME TO "IX_TournamentRegistrations_TeamId";
            ALTER INDEX competition."IX_tournament_registrations_RegisteredByUserId" RENAME TO "IX_TournamentRegistrations_RegisteredByUserId";
            ALTER INDEX competition."IX_tournament_registrations_GameId_Status_Kind" RENAME TO "IX_TournamentRegistrations_GameId_Status_Kind";
            ALTER INDEX competition."IX_roster_members_UserId" RENAME TO "IX_TournamentRegistrationRosterMembers_UserId";
            ALTER INDEX competition."IX_roster_members_TournamentRegistrationId_ConfirmationStatus" RENAME TO "IX_TournamentRegistrationRosterMembers_TournamentRegistrationI~";
            ALTER INDEX competition."IX_roster_members_TeamId" RENAME TO "IX_TournamentRegistrationRosterMembers_TeamId";
            ALTER INDEX competition."IX_roster_members_GameId_TeamId_UserId" RENAME TO "IX_TournamentRegistrationRosterMembers_GameId_TeamId_UserId";
            ALTER INDEX competition."IX_placements_GameId" RENAME TO "IX_Placements_GameId";
            ALTER INDEX competition."IX_placement_users_UserId" RENAME TO "IX_PlacementUser_UserId";
            ALTER INDEX competition."IX_placement_teams_TeamId" RENAME TO "IX_PlacementTeam_TeamId";
            ALTER INDEX competition."IX_matches_WinnerNextMatchId" RENAME TO "IX_Matches_WinnerNextMatchId";
            ALTER INDEX competition."IX_matches_UserWinnerId" RENAME TO "IX_Matches_UserWinnerId";
            ALTER INDEX competition."IX_matches_UserParticipant2Id" RENAME TO "IX_Matches_UserParticipant2Id";
            ALTER INDEX competition."IX_matches_UserParticipant1Id" RENAME TO "IX_Matches_UserParticipant1Id";
            ALTER INDEX competition."IX_matches_UserLoserId" RENAME TO "IX_Matches_UserLoserId";
            ALTER INDEX competition."IX_matches_TeamWinnerId" RENAME TO "IX_Matches_TeamWinnerId";
            ALTER INDEX competition."IX_matches_TeamParticipant2Id" RENAME TO "IX_Matches_TeamParticipant2Id";
            ALTER INDEX competition."IX_matches_TeamParticipant1Id" RENAME TO "IX_Matches_TeamParticipant1Id";
            ALTER INDEX competition."IX_matches_TeamLoserId" RENAME TO "IX_Matches_TeamLoserId";
            ALTER INDEX competition."IX_matches_LoserNextMatchId" RENAME TO "IX_Matches_LoserNextMatchId";
            ALTER INDEX competition."IX_matches_GameId" RENAME TO "IX_Matches_GameId";

            ALTER TABLE teams.teams RENAME CONSTRAINT "PK_teams" TO "PK_Teams";
            ALTER TABLE teams.teams RENAME CONSTRAINT "FK_teams_users_CaptainUserId" TO "FK_Teams_Users_CaptainUserId";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "PK_team_members" TO "PK_TeamUser";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "FK_team_members_users_UserId" TO "FK_TeamUser_Users_UserId";
            ALTER TABLE teams.team_members RENAME CONSTRAINT "FK_team_members_teams_TeamId" TO "FK_TeamUser_Teams_TeamId";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "PK_team_invites" TO "PK_TeamInvites";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "FK_team_invites_users_UserId" TO "FK_TeamInvites_Users_UserId";
            ALTER TABLE teams.team_invites RENAME CONSTRAINT "FK_team_invites_teams_TeamId" TO "FK_TeamInvites_Teams_TeamId";
            ALTER TABLE sponsorship.sponsors RENAME CONSTRAINT "PK_sponsors" TO "PK_Sponsors";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "PK_game_sponsor_placements" TO "PK_GameSponsorPlacements";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "FK_game_sponsor_placements_sponsors_SponsorId" TO "FK_GameSponsorPlacements_Sponsors_SponsorId";
            ALTER TABLE sponsorship.game_sponsor_placements RENAME CONSTRAINT "FK_game_sponsor_placements_games_GameId" TO "FK_GameSponsorPlacements_Games_GameId";
            ALTER TABLE identity.users RENAME CONSTRAINT "PK_users" TO "PK_Users";
            ALTER TABLE discovery.search_index_rebuild_documents RENAME CONSTRAINT "FK_search_index_rebuild_documents_search_index_rebuild_jobs_jo~" TO "FK_search_index_rebuild_documents_search_index_rebuild_jobs_job";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "PK_tournament_registrations" TO "PK_TournamentRegistrations";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_users_UserId" TO "FK_TournamentRegistrations_Users_UserId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_users_RegisteredByUserId" TO "FK_TournamentRegistrations_Users_RegisteredByUserId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_teams_TeamId" TO "FK_TournamentRegistrations_Teams_TeamId";
            ALTER TABLE competition.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_games_GameId" TO "FK_TournamentRegistrations_Games_GameId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "PK_roster_members" TO "PK_TournamentRegistrationRosterMembers";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_roster_members_users_UserId" TO "FK_TournamentRegistrationRosterMembers_Users_UserId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_roster_members_tournament_registrations_TournamentRegistrat~" TO "FK_TournamentRegistrationRosterMembers_TournamentRegistrations~";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_roster_members_teams_TeamId" TO "FK_TournamentRegistrationRosterMembers_Teams_TeamId";
            ALTER TABLE competition.roster_members RENAME CONSTRAINT "FK_roster_members_games_GameId" TO "FK_TournamentRegistrationRosterMembers_Games_GameId";
            ALTER TABLE competition.placements RENAME CONSTRAINT "PK_placements" TO "PK_Placements";
            ALTER TABLE competition.placements RENAME CONSTRAINT "FK_placements_games_GameId" TO "FK_Placements_Games_GameId";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "PK_placement_users" TO "PK_PlacementUser";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "FK_placement_users_users_UserId" TO "FK_PlacementUser_Users_UserId";
            ALTER TABLE competition.placement_users RENAME CONSTRAINT "FK_placement_users_placements_PlacementId" TO "FK_PlacementUser_Placements_PlacementId";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "PK_placement_teams" TO "PK_PlacementTeam";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "FK_placement_teams_teams_TeamId" TO "FK_PlacementTeam_Teams_TeamId";
            ALTER TABLE competition.placement_teams RENAME CONSTRAINT "FK_placement_teams_placements_PlacementId" TO "FK_PlacementTeam_Placements_PlacementId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "PK_matches" TO "PK_Matches";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_users_UserWinnerId" TO "FK_Matches_Users_UserWinnerId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_users_UserParticipant2Id" TO "FK_Matches_Users_UserParticipant2Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_users_UserParticipant1Id" TO "FK_Matches_Users_UserParticipant1Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_users_UserLoserId" TO "FK_Matches_Users_UserLoserId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_teams_TeamWinnerId" TO "FK_Matches_Teams_TeamWinnerId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_teams_TeamParticipant2Id" TO "FK_Matches_Teams_TeamParticipant2Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_teams_TeamParticipant1Id" TO "FK_Matches_Teams_TeamParticipant1Id";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_teams_TeamLoserId" TO "FK_Matches_Teams_TeamLoserId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_matches_WinnerNextMatchId" TO "FK_Matches_Matches_WinnerNextMatchId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_matches_LoserNextMatchId" TO "FK_Matches_Matches_LoserNextMatchId";
            ALTER TABLE competition.matches RENAME CONSTRAINT "FK_matches_games_GameId" TO "FK_Matches_Games_GameId";
            ALTER TABLE competition.games RENAME CONSTRAINT "PK_games" TO "PK_Games";

            ALTER TABLE teams.teams ALTER COLUMN "Version" SET DEFAULT 0;
            """);
    }
}
