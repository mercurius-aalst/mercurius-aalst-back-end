using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations;

/// <summary>
/// Renames the competition/game aggregate storage to tournament storage and preserves pending durable events.
/// </summary>
[DbContext(typeof(MercuriusDBContext))]
[Migration("20260827120000_RenameCompetitionGameToTournament")]
public partial class RenameCompetitionGameToTournament : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER SCHEMA competition RENAME TO tournament;

            ALTER TABLE tournament.games RENAME TO tournaments;
            ALTER TABLE sponsorship.game_sponsor_placements RENAME TO tournament_sponsor_placements;

            ALTER TABLE tournament.matches RENAME COLUMN "GameId" TO "TournamentId";
            ALTER TABLE tournament.placements RENAME COLUMN "GameId" TO "TournamentId";
            ALTER TABLE tournament.tournament_registrations RENAME COLUMN "GameId" TO "TournamentId";
            ALTER TABLE tournament.roster_members RENAME COLUMN "GameId" TO "TournamentId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME COLUMN "GameId" TO "TournamentId";

            ALTER TABLE tournament.tournaments RENAME CONSTRAINT "PK_games" TO "PK_tournaments";
            ALTER TABLE tournament.matches RENAME CONSTRAINT "FK_matches_games_GameId" TO "FK_matches_tournaments_TournamentId";
            ALTER TABLE tournament.placements RENAME CONSTRAINT "FK_placements_games_GameId" TO "FK_placements_tournaments_TournamentId";
            ALTER TABLE tournament.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_games_GameId" TO "FK_tournament_registrations_tournaments_TournamentId";
            ALTER TABLE tournament.roster_members RENAME CONSTRAINT "FK_roster_members_games_GameId" TO "FK_roster_members_tournaments_TournamentId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "PK_game_sponsor_placements" TO "PK_tournament_sponsor_placements";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "FK_game_sponsor_placements_games_GameId" TO "FK_tournament_sponsor_placements_tournaments_TournamentId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "FK_game_sponsor_placements_sponsors_SponsorId" TO "FK_tournament_sponsor_placements_sponsors_SponsorId";

            ALTER INDEX tournament."IX_matches_GameId" RENAME TO "IX_matches_TournamentId";
            ALTER INDEX tournament."IX_placements_GameId" RENAME TO "IX_placements_TournamentId";
            ALTER INDEX tournament."IX_roster_members_GameId_TeamId_UserId" RENAME TO "IX_roster_members_TournamentId_TeamId_UserId";
            ALTER INDEX tournament."IX_tournament_registrations_GameId_Status_Kind" RENAME TO "IX_tournament_registrations_TournamentId_Status_Kind";
            ALTER INDEX tournament."IX_TournamentRegistrations_GameId_RegisteredBy_PendingActive" RENAME TO "IX_TournamentRegistrations_TournamentId_RegisteredBy_PendingActive";
            ALTER INDEX tournament."IX_TournamentRegistrations_GameId_TeamId_PendingActive" RENAME TO "IX_TournamentRegistrations_TournamentId_TeamId_PendingActive";
            ALTER INDEX tournament."IX_TournamentRegistrations_GameId_UserId_PendingActive" RENAME TO "IX_TournamentRegistrations_TournamentId_UserId_PendingActive";
            ALTER INDEX tournament."IX_TournamentRosterMembers_GameId_UserId_PendingActive" RENAME TO "IX_TournamentRosterMembers_TournamentId_UserId_PendingActive";
            ALTER INDEX sponsorship."IX_game_sponsor_placements_GameId" RENAME TO "IX_tournament_sponsor_placements_TournamentId";
            ALTER INDEX sponsorship."IX_game_sponsor_placements_SponsorId" RENAME TO "IX_tournament_sponsor_placements_SponsorId";

            UPDATE platform.outbox_messages
            SET payload = ((payload::jsonb - 'gameId') || jsonb_build_object('tournamentId', payload::jsonb->'gameId'))::text
            WHERE processed_at_utc IS NULL
              AND dead_lettered_at_utc IS NULL
              AND payload::jsonb ? 'gameId'
              AND (
                  event_type LIKE 'Mercurius.Modules.Competition.Contracts.%Event%'
                  OR event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged%'
              );

            UPDATE platform.outbox_messages
            SET event_type = CASE
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameCreatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameCreatedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentCreatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameUpdatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameUpdatedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentUpdatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameStartedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameStartedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentStartedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameResetIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameResetIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentResetIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameCompletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameCompletedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentCompletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameCanceledIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameCanceledIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentCanceledIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.GameDeletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.GameDeletedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentDeletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged%' THEN REPLACE(event_type, 'Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged', 'Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.MatchCompletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.MatchCompletedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.PlacementAssignedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.PlacementAssignedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.PlacementAssignedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.RosterMemberConfirmedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.RosterMemberConfirmedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.RosterMemberConfirmedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCanceledIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCanceledIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCanceledIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCreatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCreatedIntegrationEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCreatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Competition.Contracts.TournamentRosterConfirmationChangedEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Competition.Contracts.TournamentRosterConfirmationChangedEvent', 'Mercurius.Modules.Tournament.Contracts.TournamentRosterConfirmationChangedEvent')
                ELSE event_type
            END
            WHERE processed_at_utc IS NULL
              AND dead_lettered_at_utc IS NULL
              AND (
                  event_type LIKE 'Mercurius.Modules.Competition.Contracts.%Event%'
                  OR event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged%'
              );

            UPDATE discovery.search_documents
            SET entity_type = 'tournament',
                subtitle = CASE
                    WHEN subtitle = 'Game' THEN 'Tournament'
                    ELSE subtitle
                END,
                route = CASE
                    WHEN route LIKE '/games/%' THEN regexp_replace(route, '^/games/', '/tournaments/')
                    ELSE route
                END
            WHERE entity_type = 'game';

            DROP INDEX discovery."IX_search_documents_active_exact_order";
            CREATE INDEX "IX_search_documents_active_exact_order"
                ON discovery.search_documents (normalized_text, type_order, entity_id)
                WHERE is_deleted = false AND entity_type IN ('user', 'team', 'tournament');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX discovery."IX_search_documents_active_exact_order";
            CREATE INDEX "IX_search_documents_active_exact_order"
                ON discovery.search_documents (normalized_text, type_order, entity_id)
                WHERE is_deleted = false AND entity_type IN ('user', 'team', 'game');

            UPDATE discovery.search_documents
            SET entity_type = 'game',
                subtitle = CASE
                    WHEN subtitle = 'Tournament' THEN 'Game'
                    ELSE subtitle
                END,
                route = CASE
                    WHEN route LIKE '/tournaments/%' THEN regexp_replace(route, '^/tournaments/', '/games/')
                    ELSE route
                END
            WHERE entity_type = 'tournament';

            UPDATE platform.outbox_messages
            SET payload = ((payload::jsonb - 'tournamentId') || jsonb_build_object('gameId', payload::jsonb->'tournamentId'))::text
            WHERE processed_at_utc IS NULL
              AND dead_lettered_at_utc IS NULL
              AND payload::jsonb ? 'tournamentId'
              AND (
                  event_type LIKE 'Mercurius.Modules.Tournament.Contracts.%Event%'
                  OR event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged%'
              );

            UPDATE platform.outbox_messages
            SET event_type = CASE
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentCreatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentCreatedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameCreatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentUpdatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentUpdatedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameUpdatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentStartedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentStartedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameStartedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentResetIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentResetIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameResetIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentCompletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentCompletedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameCompletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentCanceledIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentCanceledIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameCanceledIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentDeletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentDeletedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.GameDeletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged%' THEN REPLACE(event_type, 'Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged', 'Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.MatchCompletedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.PlacementAssignedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.PlacementAssignedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.PlacementAssignedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.RosterMemberConfirmedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.RosterMemberConfirmedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.RosterMemberConfirmedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCanceledIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCanceledIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCanceledIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCreatedIntegrationEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCreatedIntegrationEvent', 'Mercurius.Modules.Competition.Contracts.TournamentRegistrationCreatedIntegrationEvent')
                WHEN event_type LIKE 'Mercurius.Modules.Tournament.Contracts.TournamentRosterConfirmationChangedEvent%' THEN REPLACE(event_type, 'Mercurius.Modules.Tournament.Contracts.TournamentRosterConfirmationChangedEvent', 'Mercurius.Modules.Competition.Contracts.TournamentRosterConfirmationChangedEvent')
                ELSE event_type
            END
            WHERE processed_at_utc IS NULL
              AND dead_lettered_at_utc IS NULL
              AND (
                  event_type LIKE 'Mercurius.Modules.Tournament.Contracts.%Event%'
                  OR event_type LIKE 'Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged%'
              );

            ALTER INDEX sponsorship."IX_tournament_sponsor_placements_SponsorId" RENAME TO "IX_game_sponsor_placements_SponsorId";
            ALTER INDEX sponsorship."IX_tournament_sponsor_placements_TournamentId" RENAME TO "IX_game_sponsor_placements_GameId";
            ALTER INDEX tournament."IX_TournamentRosterMembers_TournamentId_UserId_PendingActive" RENAME TO "IX_TournamentRosterMembers_GameId_UserId_PendingActive";
            ALTER INDEX tournament."IX_TournamentRegistrations_TournamentId_UserId_PendingActive" RENAME TO "IX_TournamentRegistrations_GameId_UserId_PendingActive";
            ALTER INDEX tournament."IX_TournamentRegistrations_TournamentId_TeamId_PendingActive" RENAME TO "IX_TournamentRegistrations_GameId_TeamId_PendingActive";
            ALTER INDEX tournament."IX_TournamentRegistrations_TournamentId_RegisteredBy_PendingActive" RENAME TO "IX_TournamentRegistrations_GameId_RegisteredBy_PendingActive";
            ALTER INDEX tournament."IX_tournament_registrations_TournamentId_Status_Kind" RENAME TO "IX_tournament_registrations_GameId_Status_Kind";
            ALTER INDEX tournament."IX_roster_members_TournamentId_TeamId_UserId" RENAME TO "IX_roster_members_GameId_TeamId_UserId";
            ALTER INDEX tournament."IX_placements_TournamentId" RENAME TO "IX_placements_GameId";
            ALTER INDEX tournament."IX_matches_TournamentId" RENAME TO "IX_matches_GameId";

            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "FK_tournament_sponsor_placements_sponsors_SponsorId" TO "FK_game_sponsor_placements_sponsors_SponsorId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "FK_tournament_sponsor_placements_tournaments_TournamentId" TO "FK_game_sponsor_placements_games_GameId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME CONSTRAINT "PK_tournament_sponsor_placements" TO "PK_game_sponsor_placements";
            ALTER TABLE tournament.roster_members RENAME CONSTRAINT "FK_roster_members_tournaments_TournamentId" TO "FK_roster_members_games_GameId";
            ALTER TABLE tournament.tournament_registrations RENAME CONSTRAINT "FK_tournament_registrations_tournaments_TournamentId" TO "FK_tournament_registrations_games_GameId";
            ALTER TABLE tournament.placements RENAME CONSTRAINT "FK_placements_tournaments_TournamentId" TO "FK_placements_games_GameId";
            ALTER TABLE tournament.matches RENAME CONSTRAINT "FK_matches_tournaments_TournamentId" TO "FK_matches_games_GameId";
            ALTER TABLE tournament.tournaments RENAME CONSTRAINT "PK_tournaments" TO "PK_games";

            ALTER TABLE tournament.matches RENAME COLUMN "TournamentId" TO "GameId";
            ALTER TABLE tournament.placements RENAME COLUMN "TournamentId" TO "GameId";
            ALTER TABLE tournament.tournament_registrations RENAME COLUMN "TournamentId" TO "GameId";
            ALTER TABLE tournament.roster_members RENAME COLUMN "TournamentId" TO "GameId";
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME COLUMN "TournamentId" TO "GameId";

            ALTER TABLE tournament.tournaments RENAME TO games;
            ALTER TABLE sponsorship.tournament_sponsor_placements RENAME TO game_sponsor_placements;
            ALTER SCHEMA tournament RENAME TO competition;
            """);
    }
}
