using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mercurius.LAN.API.Migrations;

/// <summary>
/// Hand-authored migration for Phase 11. The EF model snapshot is synchronized by a later migration.
/// </summary>
[DbContext(typeof(MercuriusDBContext))]
[Migration("20260729123000_CompetitionRegistrationSnapshots")]
public partial class CompetitionRegistrationSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "TournamentRegistrations"
                ADD COLUMN IF NOT EXISTS "RegisteredByUsernameAtRegistration" character varying(32);

            ALTER TABLE "TournamentRegistrations"
                ADD COLUMN IF NOT EXISTS "UsernameAtRegistration" character varying(32);

            ALTER TABLE "TournamentRegistrations"
                ADD COLUMN IF NOT EXISTS "TeamNameAtRegistration" character varying(100);

            ALTER TABLE "TournamentRegistrations"
                ADD COLUMN IF NOT EXISTS "TeamCaptainUserIdAtRegistration" uuid;

            ALTER TABLE "TournamentRegistrations"
                ADD COLUMN IF NOT EXISTS "TeamLogoUrlAtRegistration" character varying(260);

            ALTER TABLE "TournamentRegistrationRosterMembers"
                ADD COLUMN IF NOT EXISTS "UsernameAtRegistration" character varying(32) NOT NULL DEFAULT '';

            ALTER TABLE "TournamentRegistrationRosterMembers"
                ADD COLUMN IF NOT EXISTS "DisplayNameAtRegistration" character varying(200) NOT NULL DEFAULT '';

            ALTER TABLE "TournamentRegistrationRosterMembers"
                ADD COLUMN IF NOT EXISTS "TeamNameAtRegistration" character varying(100);
            """);

        migrationBuilder.Sql(
            """
            UPDATE "TournamentRegistrations" AS registration
            SET "RegisteredByUsernameAtRegistration" = COALESCE(registered_by."Username", '')
            FROM "Users" AS registered_by
            WHERE registration."RegisteredByUserId" = registered_by."Id"
              AND COALESCE(registration."RegisteredByUsernameAtRegistration", '') = '';

            UPDATE "TournamentRegistrations" AS registration
            SET "UsernameAtRegistration" = user_profile."Username"
            FROM "Users" AS user_profile
            WHERE registration."UserId" = user_profile."Id"
              AND registration."UserId" IS NOT NULL
              AND registration."UsernameAtRegistration" IS NULL;

            UPDATE "TournamentRegistrations" AS registration
            SET
                "TeamNameAtRegistration" = team."Name",
                "TeamCaptainUserIdAtRegistration" = team."CaptainUserId",
                "TeamLogoUrlAtRegistration" = team."LogoUrl"
            FROM "Teams" AS team
            WHERE registration."TeamId" = team."Id"
              AND registration."TeamId" IS NOT NULL
              AND (
                  registration."TeamNameAtRegistration" IS NULL
                  OR registration."TeamCaptainUserIdAtRegistration" IS NULL
                  OR registration."TeamLogoUrlAtRegistration" IS NULL
              );

            UPDATE "TournamentRegistrationRosterMembers" AS member
            SET
                "UsernameAtRegistration" = COALESCE(user_profile."Username", ''),
                "DisplayNameAtRegistration" = CASE
                    WHEN user_profile."IsDeleted" THEN 'Deleted user'
                    WHEN NULLIF(BTRIM(COALESCE(user_profile."Firstname", '') || ' ' || COALESCE(user_profile."Lastname", '')), '') IS NOT NULL
                        THEN BTRIM(COALESCE(user_profile."Firstname", '') || ' ' || COALESCE(user_profile."Lastname", ''))
                    ELSE COALESCE(user_profile."Username", 'Incomplete profile')
                END
            FROM "Users" AS user_profile
            WHERE member."UserId" = user_profile."Id"
              AND (
                  COALESCE(member."UsernameAtRegistration", '') = ''
                  OR COALESCE(member."DisplayNameAtRegistration", '') = ''
              );

            UPDATE "TournamentRegistrationRosterMembers" AS member
            SET "TeamNameAtRegistration" = team."Name"
            FROM "Teams" AS team
            WHERE member."TeamId" = team."Id"
              AND member."TeamId" IS NOT NULL
              AND member."TeamNameAtRegistration" IS NULL;

            UPDATE "TournamentRegistrations"
            SET "RegisteredByUsernameAtRegistration" = ''
            WHERE "RegisteredByUsernameAtRegistration" IS NULL;

            ALTER TABLE "TournamentRegistrations"
                ALTER COLUMN "RegisteredByUsernameAtRegistration" SET NOT NULL;

            ALTER TABLE "TournamentRegistrationRosterMembers"
                ALTER COLUMN "UsernameAtRegistration" DROP DEFAULT;

            ALTER TABLE "TournamentRegistrationRosterMembers"
                ALTER COLUMN "DisplayNameAtRegistration" DROP DEFAULT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "TournamentRegistrationRosterMembers"
                DROP COLUMN IF EXISTS "TeamNameAtRegistration";

            ALTER TABLE "TournamentRegistrationRosterMembers"
                DROP COLUMN IF EXISTS "DisplayNameAtRegistration";

            ALTER TABLE "TournamentRegistrationRosterMembers"
                DROP COLUMN IF EXISTS "UsernameAtRegistration";

            ALTER TABLE "TournamentRegistrations"
                DROP COLUMN IF EXISTS "TeamLogoUrlAtRegistration";

            ALTER TABLE "TournamentRegistrations"
                DROP COLUMN IF EXISTS "TeamCaptainUserIdAtRegistration";

            ALTER TABLE "TournamentRegistrations"
                DROP COLUMN IF EXISTS "TeamNameAtRegistration";

            ALTER TABLE "TournamentRegistrations"
                DROP COLUMN IF EXISTS "UsernameAtRegistration";

            ALTER TABLE "TournamentRegistrations"
                DROP COLUMN IF EXISTS "RegisteredByUsernameAtRegistration";
            """);
    }
}
