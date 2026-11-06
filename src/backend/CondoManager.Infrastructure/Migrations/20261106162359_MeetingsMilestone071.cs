using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MeetingsMilestone071 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingAbsences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAbsences", x => x.Id);
                    table.UniqueConstraint("AK_MeetingAbsences_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingAbsences_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingAbsences_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingAbsences_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAbsences_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAbsences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    RelatedType = table.Column<int>(type: "integer", nullable: true),
                    RelatedId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAgendaItems", x => x.Id);
                    table.UniqueConstraint("AK_MeetingAgendaItems_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingAgendaItems_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingAgendaItems_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Agenda_Position", "\"Position\">0 AND ((\"RelatedId\" IS NULL)=(\"RelatedType\" IS NULL))");
                    table.ForeignKey(
                        name: "FK_MeetingAgendaItems_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAgendaItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ProxyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RepresentedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendances", x => x.Id);
                    table.UniqueConstraint("AK_MeetingAttendances_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingAttendances_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingAttendances_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Attendance_Dates", "\"CheckOutAt\" IS NULL OR \"CheckOutAt\">=\"CheckInAt\"");
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_People_TenantId_RepresentedById",
                        columns: x => new { x.TenantId, x.RepresentedById },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAttendanceUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Millesimals = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendanceUnits", x => x.Id);
                    table.UniqueConstraint("AK_MeetingAttendanceUnits_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingAttendanceUnits_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingAttendanceUnits_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingAttendanceUnits_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendanceUnits_MeetingAttendances_TenantId_Condomin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.AttendanceId },
                        principalTable: "MeetingAttendances",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendanceUnits_PropertyUnits_TenantId_CondominiumId~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendanceUnits_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingNoticeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeclaredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeclaredBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNoticeEvents", x => x.Id);
                    table.UniqueConstraint("AK_MeetingNoticeEvents_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingNoticeEvents_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingNoticeEvents_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingNoticeEvents_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingNoticeEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingNotices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Contact = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNotices", x => x.Id);
                    table.UniqueConstraint("AK_MeetingNotices_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingNotices_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingNotices_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingNotices_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingNotices_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingNotices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingProxies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DelegateName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DocumentReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingProxies", x => x.Id);
                    table.UniqueConstraint("AK_MeetingProxies_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingProxies_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingProxies_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Proxy_People", "\"DelegatorId\"<>\"DelegateId\" AND \"Status\" BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_MeetingProxies_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxies_People_TenantId_DelegateId",
                        columns: x => new { x.TenantId, x.DelegateId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxies_People_TenantId_DelegatorId",
                        columns: x => new { x.TenantId, x.DelegatorId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingProxyUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProxyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingProxyUnits", x => x.Id);
                    table.UniqueConstraint("AK_MeetingProxyUnits_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingProxyUnits_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingProxyUnits_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingProxyUnits_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxyUnits_MeetingProxies_TenantId_CondominiumId_Mee~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.ProxyId },
                        principalTable: "MeetingProxies",
                        principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxyUnits_PropertyUnits_TenantId_CondominiumId_Prop~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingProxyUnits_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRevisions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingRevisions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingRevisions_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingRevisions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingRevisions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingRevisions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                    Location = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RemoteMeetingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CurrentSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.UniqueConstraint("AK_Meetings_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Meetings_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Meeting_State", "\"Version\">0 AND \"Type\" BETWEEN 0 AND 1 AND \"Status\" BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_Meetings_AccountingPeriods_TenantId_CondominiumId_Accountin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Call = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSessions", x => x.Id);
                    table.UniqueConstraint("AK_MeetingSessions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingSessions_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingSessions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_MeetingSession_State", "\"Call\" BETWEEN 0 AND 1 AND \"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_MeetingSessions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSessions_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ReferenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AllocationTableId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSnapshots", x => x.Id);
                    table.UniqueConstraint("AK_MeetingSnapshots_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingSnapshots_TenantId_CondominiumId_MeetingId_Id", x => new { x.TenantId, x.CondominiumId, x.MeetingId, x.Id });
                    table.UniqueConstraint("AK_MeetingSnapshots_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeetingSnapshots_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSnapshots_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSnapshots_MillesimalTables_TenantId_CondominiumId_Al~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AllocationTableId },
                        principalTable: "MillesimalTables",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSnapshots_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAbsences_TenantId_CondominiumId",
                table: "MeetingAbsences",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAbsences_TenantId_CondominiumId_MeetingId_SessionId",
                table: "MeetingAbsences",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAbsences_TenantId_PersonId",
                table: "MeetingAbsences",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaItems_TenantId_CondominiumId",
                table: "MeetingAgendaItems",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaItems_TenantId_MeetingId_Position",
                table: "MeetingAgendaItems",
                columns: new[] { "TenantId", "MeetingId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_CondominiumId",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_CondominiumId_MeetingId_ProxyId",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "ProxyId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_CondominiumId_MeetingId_Session~",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_CondominiumId_MeetingId_Snapsho~",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_PersonId",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_TenantId_RepresentedById",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "RepresentedById" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendanceUnits_TenantId_CondominiumId",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendanceUnits_TenantId_CondominiumId_MeetingId_Att~",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "AttendanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendanceUnits_TenantId_CondominiumId_MeetingId_Ses~",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendanceUnits_TenantId_CondominiumId_PropertyUnitId",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendanceUnits_TenantId_SessionId_PropertyUnitId",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "SessionId", "PropertyUnitId" },
                unique: true,
                filter: "\"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNoticeEvents_TenantId_CondominiumId",
                table: "MeetingNoticeEvents",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNoticeEvents_TenantId_CondominiumId_MeetingId_Notice~",
                table: "MeetingNoticeEvents",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "NoticeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotices_TenantId_CondominiumId",
                table: "MeetingNotices",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotices_TenantId_CondominiumId_MeetingId_SnapshotId",
                table: "MeetingNotices",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotices_TenantId_PersonId",
                table: "MeetingNotices",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxies_TenantId_CondominiumId",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxies_TenantId_CondominiumId_MeetingId_SnapshotId",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxies_TenantId_DelegateId",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "DelegateId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxies_TenantId_DelegatorId",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "DelegatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxyUnits_TenantId_CondominiumId",
                table: "MeetingProxyUnits",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxyUnits_TenantId_CondominiumId_MeetingId_ProxyId",
                table: "MeetingProxyUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "ProxyId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxyUnits_TenantId_CondominiumId_PropertyUnitId",
                table: "MeetingProxyUnits",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProxyUnits_TenantId_MeetingId_PropertyUnitId",
                table: "MeetingProxyUnits",
                columns: new[] { "TenantId", "MeetingId", "PropertyUnitId" },
                unique: true,
                filter: "\"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRevisions_TenantId_CondominiumId",
                table: "MeetingRevisions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRevisions_TenantId_MeetingId_Version",
                table: "MeetingRevisions",
                columns: new[] { "TenantId", "MeetingId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId_CondominiumId",
                table: "Meetings",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId_CondominiumId_AccountingPeriodId_Status",
                table: "Meetings",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId_CondominiumId_Id_CurrentSnapshotId",
                table: "Meetings",
                columns: new[] { "TenantId", "CondominiumId", "Id", "CurrentSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSessions_TenantId_CondominiumId",
                table: "MeetingSessions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSessions_TenantId_MeetingId_Call",
                table: "MeetingSessions",
                columns: new[] { "TenantId", "MeetingId", "Call" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSnapshots_TenantId_CondominiumId",
                table: "MeetingSnapshots",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSnapshots_TenantId_CondominiumId_AllocationTableId",
                table: "MeetingSnapshots",
                columns: new[] { "TenantId", "CondominiumId", "AllocationTableId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSnapshots_TenantId_MeetingId_Version",
                table: "MeetingSnapshots",
                columns: new[] { "TenantId", "MeetingId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAbsences_MeetingSessions_TenantId_CondominiumId_Meet~",
                table: "MeetingAbsences",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" },
                principalTable: "MeetingSessions",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAbsences_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingAbsences",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAgendaItems_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingAgendaItems",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendances_MeetingProxies_TenantId_CondominiumId_Me~",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "ProxyId" },
                principalTable: "MeetingProxies",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendances_MeetingSessions_TenantId_CondominiumId_M~",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" },
                principalTable: "MeetingSessions",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendances_MeetingSnapshots_TenantId_CondominiumId_~",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" },
                principalTable: "MeetingSnapshots",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendances_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingAttendances",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendanceUnits_MeetingSessions_TenantId_Condominium~",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SessionId" },
                principalTable: "MeetingSessions",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttendanceUnits_Meetings_TenantId_CondominiumId_Meet~",
                table: "MeetingAttendanceUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNoticeEvents_MeetingNotices_TenantId_CondominiumId_M~",
                table: "MeetingNoticeEvents",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "NoticeId" },
                principalTable: "MeetingNotices",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNoticeEvents_Meetings_TenantId_CondominiumId_Meeting~",
                table: "MeetingNoticeEvents",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNotices_MeetingSnapshots_TenantId_CondominiumId_Meet~",
                table: "MeetingNotices",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" },
                principalTable: "MeetingSnapshots",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNotices_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingNotices",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingProxies_MeetingSnapshots_TenantId_CondominiumId_Meet~",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId", "SnapshotId" },
                principalTable: "MeetingSnapshots",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingProxies_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingProxies",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingProxyUnits_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingProxyUnits",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRevisions_Meetings_TenantId_CondominiumId_MeetingId",
                table: "MeetingRevisions",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" },
                principalTable: "Meetings",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_MeetingSnapshots_TenantId_CondominiumId_Id_Current~",
                table: "Meetings",
                columns: new[] { "TenantId", "CondominiumId", "Id", "CurrentSnapshotId" },
                principalTable: "MeetingSnapshots",
                principalColumns: new[] { "TenantId", "CondominiumId", "MeetingId", "Id" },
                onDelete: ReferentialAction.Restrict);
            MeetingConstraints(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Forward-only: preserve assembly history.");
    }
}