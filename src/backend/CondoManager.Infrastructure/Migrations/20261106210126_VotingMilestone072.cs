using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VotingMilestone072 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingAgendaDispositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Disposition = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAgendaDispositions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingAgendaDispositions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingAgendaDispositions_TenantId_CondominiumId_MeetingId_~", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingAgendaDispositions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingAgendaDispositions_Condominiums_TenantId_Condominium~",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAgendaDispositions_MeetingAgendaItems_TenantId_Condo~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.AgendaItemId },
                        principalTable: "MeetingAgendaItems",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAgendaDispositions_Meetings_TenantId_CondominiumId_M~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAgendaDispositions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingCompletions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingCompletions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingCompletions_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingCompletions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingCompletions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingCompletions_MeetingSessions_TenantId_CondominiumId_M~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.SessionId },
                        principalTable: "MeetingSessions",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingCompletions_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingCompletions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingDecisionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Operation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingDecisionRequests", x => x.Id);
                    table.UniqueConstraint("AK_MeetingDecisionRequests_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingDecisionRequests_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingDecisionRequests_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingDecisionRequests_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingDecisionRequests_Meetings_TenantId_CondominiumId_Mee~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingDecisionRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VotingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingRules", x => x.Id);
                    table.UniqueConstraint("AK_VotingRules_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_VotingRules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_VotingRule_Version", "\"Version\">0 AND \"Status\" BETWEEN 0 AND 2 AND (\"EffectiveTo\" IS NULL OR \"EffectiveTo\">=\"EffectiveFrom\")");
                    table.ForeignKey(
                        name: "FK_VotingRules_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VotingRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VotingRules_VotingRules_TenantId_CondominiumId_PreviousVers~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousVersionId },
                        principalTable: "VotingRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingQuorumChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingQuorumChecks", x => x.Id);
                    table.UniqueConstraint("AK_MeetingQuorumChecks_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingQuorumChecks_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingQuorumChecks_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_MeetingSessions_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.SessionId },
                        principalTable: "MeetingSessions",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_MeetingSnapshots_TenantId_CondominiumId~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.SnapshotId },
                        principalTable: "MeetingSnapshots",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_Meetings_TenantId_CondominiumId_Meeting~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingQuorumChecks_VotingRules_TenantId_CondominiumId_Rule~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "VotingRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleVersion = table.Column<int>(type: "integer", nullable: false),
                    QuorumCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalizedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ElectorateJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingVotes", x => x.Id);
                    table.UniqueConstraint("AK_MeetingVotes_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingVotes_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingVotes_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_MeetingVote_Status", "\"Status\" BETWEEN 0 AND 4 AND \"RuleVersion\">0");
                    table.ForeignKey(
                        name: "FK_MeetingVotes_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_MeetingAgendaItems_TenantId_CondominiumId_Meet~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.AgendaItemId },
                        principalTable: "MeetingAgendaItems",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_MeetingQuorumChecks_TenantId_CondominiumId_Mee~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.QuorumCheckId },
                        principalTable: "MeetingQuorumChecks",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_MeetingSessions_TenantId_CondominiumId_Meeting~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.SessionId },
                        principalTable: "MeetingSessions",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_MeetingSnapshots_TenantId_CondominiumId_Meetin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.SnapshotId },
                        principalTable: "MeetingSnapshots",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_VotingRules_TenantId_CondominiumId_RuleId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "VotingRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeadKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpressedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Millesimals = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Choice = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingBallots", x => x.Id);
                    table.UniqueConstraint("AK_MeetingBallots_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingBallots_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingBallots_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Ballot_Choice", "\"Choice\" BETWEEN 0 AND 2 AND \"Millesimals\">=0 AND \"Revision\">0");
                    table.ForeignKey(
                        name: "FK_MeetingBallots_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallots_MeetingVotes_TenantId_CondominiumId_MeetingI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.VoteId },
                        principalTable: "MeetingVotes",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallots_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallots_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ResolutionText = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    FinalizedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousResolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingResolutions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingResolutions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingResolutions_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingResolutions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_MeetingAgendaItems_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.AgendaItemId },
                        principalTable: "MeetingAgendaItems",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_MeetingResolutions_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.PreviousResolutionId },
                        principalTable: "MeetingResolutions",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_MeetingVotes_TenantId_CondominiumId_Meet~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.VoteId },
                        principalTable: "MeetingVotes",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingResolutions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingBallotRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Choice = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingBallotRevisions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingBallotRevisions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingBallotRevisions_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingBallotRevisions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingBallotRevisions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallotRevisions_MeetingBallots_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.BallotId },
                        principalTable: "MeetingBallots",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallotRevisions_MeetingVotes_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.VoteId },
                        principalTable: "MeetingVotes",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallotRevisions_Meetings_TenantId_CondominiumId_Meet~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingBallotRevisions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResolutionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedType = table.Column<int>(type: "integer", nullable: false),
                    RelatedId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetFingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HistoricalOnly = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolutionApplications", x => x.Id);
                    table.UniqueConstraint("AK_ResolutionApplications_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ResolutionApplications_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_ResolutionApplications_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ResolutionApplications_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolutionApplications_MeetingResolutions_TenantId_Condomin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.ResolutionId },
                        principalTable: "MeetingResolutions",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolutionApplications_Meetings_TenantId_CondominiumId_Meet~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolutionApplications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaDispositions_TenantId_CondominiumId",
                table: "MeetingAgendaDispositions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaDispositions_TenantId_CondominiumId_MeetingId_~",
                table: "MeetingAgendaDispositions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "AgendaItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaDispositions_TenantId_MeetingId_AgendaItemId",
                table: "MeetingAgendaDispositions",
                columns: new[] { "TenantId", "MeetingId", "AgendaItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallotRevisions_TenantId_BallotId_Version",
                table: "MeetingBallotRevisions",
                columns: new[] { "TenantId", "BallotId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallotRevisions_TenantId_CondominiumId",
                table: "MeetingBallotRevisions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallotRevisions_TenantId_CondominiumId_MeetingId_Bal~",
                table: "MeetingBallotRevisions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "BallotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallotRevisions_TenantId_CondominiumId_MeetingId_Vot~",
                table: "MeetingBallotRevisions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "VoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallots_TenantId_CondominiumId",
                table: "MeetingBallots",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallots_TenantId_CondominiumId_MeetingId_VoteId",
                table: "MeetingBallots",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "VoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBallots_TenantId_VoteId_HeadKey",
                table: "MeetingBallots",
                columns: new[] { "TenantId", "VoteId", "HeadKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCompletions_TenantId_CondominiumId",
                table: "MeetingCompletions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCompletions_TenantId_CondominiumId_MeetingId_Session~",
                table: "MeetingCompletions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCompletions_TenantId_MeetingId",
                table: "MeetingCompletions",
                columns: new[] { "TenantId", "MeetingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecisionRequests_TenantId_CondominiumId",
                table: "MeetingDecisionRequests",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecisionRequests_TenantId_MeetingId_RequestId",
                table: "MeetingDecisionRequests",
                columns: new[] { "TenantId", "MeetingId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingQuorumChecks_TenantId_CondominiumId",
                table: "MeetingQuorumChecks",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingQuorumChecks_TenantId_CondominiumId_MeetingId_Sessio~",
                table: "MeetingQuorumChecks",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingQuorumChecks_TenantId_CondominiumId_MeetingId_Snapsh~",
                table: "MeetingQuorumChecks",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingQuorumChecks_TenantId_CondominiumId_RuleId",
                table: "MeetingQuorumChecks",
                columns: new[] { "TenantId", "CondominiumId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_PreviousResolutionId",
                table: "MeetingResolutions",
                column: "PreviousResolutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_CondominiumId",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_CondominiumId_MeetingId_AgendaI~",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "AgendaItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_CondominiumId_MeetingId_Previou~",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "PreviousResolutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_CondominiumId_MeetingId_VoteId",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "VoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_MeetingId_RequestId",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "MeetingId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingResolutions_TenantId_VoteId_Version",
                table: "MeetingResolutions",
                columns: new[] { "TenantId", "VoteId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId_MeetingId_AgendaItemId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "AgendaItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId_MeetingId_QuorumCheckId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "QuorumCheckId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId_MeetingId_SessionId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId_MeetingId_SnapshotId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_CondominiumId_RuleId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "CondominiumId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_MeetingId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "MeetingId" },
                unique: true,
                filter: "\"Status\"=1");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_TenantId_MeetingId_SessionId_AgendaItemId",
                table: "MeetingVotes",
                columns: new[] { "TenantId", "MeetingId", "SessionId", "AgendaItemId" },
                unique: true,
                filter: "\"Status\"=1");

            migrationBuilder.CreateIndex(
                name: "IX_ResolutionApplications_TenantId_CondominiumId",
                table: "ResolutionApplications",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResolutionApplications_TenantId_CondominiumId_MeetingId_Res~",
                table: "ResolutionApplications",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "ResolutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResolutionApplications_TenantId_CondominiumId_RelatedType_R~",
                table: "ResolutionApplications",
                columns: new[] { "TenantId", "CondominiumId", "RelatedType", "RelatedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResolutionApplications_TenantId_MeetingId_RequestId",
                table: "ResolutionApplications",
                columns: new[] { "TenantId", "MeetingId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingRules_PreviousVersionId",
                table: "VotingRules",
                column: "PreviousVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingRules_TenantId_CondominiumId",
                table: "VotingRules",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_VotingRules_TenantId_CondominiumId_Code_Version",
                table: "VotingRules",
                columns: new[] { "TenantId", "CondominiumId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingRules_TenantId_CondominiumId_PreviousVersionId",
                table: "VotingRules",
                columns: new[] { "TenantId", "CondominiumId", "PreviousVersionId" });
            VotingConstraints(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Forward-only: preserve votes and resolutions.");
    }
}