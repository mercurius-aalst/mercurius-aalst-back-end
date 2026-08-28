using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations;

[DbContext(typeof(MercuriusDBContext))]
[Migration("20260807143500_ModuleSchemaOwnership")]
public partial class ModuleSchemaOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "identity");
        migrationBuilder.EnsureSchema(name: "teams");
        migrationBuilder.EnsureSchema(name: "competition");
        migrationBuilder.EnsureSchema(name: "sponsorship");

        migrationBuilder.Sql("""
            ALTER TABLE public."Users" SET SCHEMA identity;
            ALTER TABLE identity."Users" RENAME TO users;

            ALTER TABLE public."Teams" SET SCHEMA teams;
            ALTER TABLE teams."Teams" RENAME TO teams;
            ALTER TABLE public."TeamInvites" SET SCHEMA teams;
            ALTER TABLE teams."TeamInvites" RENAME TO team_invites;
            ALTER TABLE public."TeamUser" SET SCHEMA teams;
            ALTER TABLE teams."TeamUser" RENAME TO team_members;

            ALTER TABLE public."Games" SET SCHEMA competition;
            ALTER TABLE competition."Games" RENAME TO games;
            ALTER TABLE public."Matches" SET SCHEMA competition;
            ALTER TABLE competition."Matches" RENAME TO matches;
            ALTER TABLE public."TournamentRegistrations" SET SCHEMA competition;
            ALTER TABLE competition."TournamentRegistrations" RENAME TO tournament_registrations;
            ALTER TABLE public."TournamentRegistrationRosterMembers" SET SCHEMA competition;
            ALTER TABLE competition."TournamentRegistrationRosterMembers" RENAME TO roster_members;
            ALTER TABLE public."Placements" SET SCHEMA competition;
            ALTER TABLE competition."Placements" RENAME TO placements;
            ALTER TABLE public."PlacementUser" SET SCHEMA competition;
            ALTER TABLE competition."PlacementUser" RENAME TO placement_users;
            ALTER TABLE public."PlacementTeam" SET SCHEMA competition;
            ALTER TABLE competition."PlacementTeam" RENAME TO placement_teams;

            ALTER TABLE public."Sponsors" SET SCHEMA sponsorship;
            ALTER TABLE sponsorship."Sponsors" RENAME TO sponsors;
            ALTER TABLE public."GameSponsorPlacements" SET SCHEMA sponsorship;
            ALTER TABLE sponsorship."GameSponsorPlacements" RENAME TO game_sponsor_placements;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE identity.users SET SCHEMA public;
            ALTER TABLE public.users RENAME TO "Users";

            ALTER TABLE teams.teams SET SCHEMA public;
            ALTER TABLE public.teams RENAME TO "Teams";
            ALTER TABLE teams.team_invites SET SCHEMA public;
            ALTER TABLE public.team_invites RENAME TO "TeamInvites";
            ALTER TABLE teams.team_members SET SCHEMA public;
            ALTER TABLE public.team_members RENAME TO "TeamUser";

            ALTER TABLE competition.games SET SCHEMA public;
            ALTER TABLE public.games RENAME TO "Games";
            ALTER TABLE competition.matches SET SCHEMA public;
            ALTER TABLE public.matches RENAME TO "Matches";
            ALTER TABLE competition.tournament_registrations SET SCHEMA public;
            ALTER TABLE public.tournament_registrations RENAME TO "TournamentRegistrations";
            ALTER TABLE competition.roster_members SET SCHEMA public;
            ALTER TABLE public.roster_members RENAME TO "TournamentRegistrationRosterMembers";
            ALTER TABLE competition.placements SET SCHEMA public;
            ALTER TABLE public.placements RENAME TO "Placements";
            ALTER TABLE competition.placement_users SET SCHEMA public;
            ALTER TABLE public.placement_users RENAME TO "PlacementUser";
            ALTER TABLE competition.placement_teams SET SCHEMA public;
            ALTER TABLE public.placement_teams RENAME TO "PlacementTeam";

            ALTER TABLE sponsorship.sponsors SET SCHEMA public;
            ALTER TABLE public.sponsors RENAME TO "Sponsors";
            ALTER TABLE sponsorship.game_sponsor_placements SET SCHEMA public;
            ALTER TABLE public.game_sponsor_placements RENAME TO "GameSponsorPlacements";

            DROP SCHEMA sponsorship;
            DROP SCHEMA competition;
            DROP SCHEMA teams;
            DROP SCHEMA identity;
            """);
    }
}
