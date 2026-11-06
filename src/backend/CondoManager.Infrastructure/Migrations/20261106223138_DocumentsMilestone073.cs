using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentsMilestone073 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OperationalOnly = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.UniqueConstraint("AK_Documents_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Documents_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Document_Metadata", "\"SizeBytes\">0 AND \"Version\">0 AND length(\"Sha256\")=64 AND \"Status\" BETWEEN 0 AND 3 AND \"Type\" BETWEEN 0 AND 9");
                    table.ForeignKey(
                        name: "FK_Documents_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_TenantId_CondominiumId_PreviousDocument~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousDocumentId },
                        principalTable: "Documents",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLinks", x => x.Id);
                    table.UniqueConstraint("AK_DocumentLinks_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_DocumentLinks_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DocumentLinks_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentLinks_Documents_TenantId_CondominiumId_DocumentId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.DocumentId },
                        principalTable: "Documents",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentLinks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingMinutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreviousMinutesId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Introduction = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    DiscussionNotes = table.Column<string>(type: "character varying(60000)", maxLength: 60000, nullable: false),
                    ClosingNotes = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    SourceJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PreparedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalizedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingMinutes", x => x.Id);
                    table.UniqueConstraint("AK_MeetingMinutes_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_MeetingMinutes_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Minutes_State", "\"Version\">0 AND \"Revision\">0 AND \"Status\" BETWEEN 0 AND 3 AND ((\"Status\" IN (2,3)) = (\"DocumentId\" IS NOT NULL AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedBy\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Documents_TenantId_CondominiumId_DocumentId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.DocumentId },
                        principalTable: "Documents",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_MeetingMinutes_TenantId_CondominiumId_Previo~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousMinutesId },
                        principalTable: "MeetingMinutes",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Meetings_TenantId_CondominiumId_MeetingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.MeetingId },
                        principalTable: "Meetings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_TenantId_CondominiumId_CreatedAt",
                table: "DocumentLinks",
                columns: new[] { "TenantId", "CondominiumId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_TenantId_CondominiumId_DocumentId",
                table: "DocumentLinks",
                columns: new[] { "TenantId", "CondominiumId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_TenantId_CondominiumId_TargetType_TargetId",
                table: "DocumentLinks",
                columns: new[] { "TenantId", "CondominiumId", "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_TenantId_DocumentId_TargetType_TargetId",
                table: "DocumentLinks",
                columns: new[] { "TenantId", "DocumentId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_CondominiumId_CreatedAt",
                table: "Documents",
                columns: new[] { "TenantId", "CondominiumId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_CondominiumId_PreviousDocumentId",
                table: "Documents",
                columns: new[] { "TenantId", "CondominiumId", "PreviousDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_CondominiumId_RequestId",
                table: "Documents",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_StorageKey",
                table: "Documents",
                columns: new[] { "TenantId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_CondominiumId_CreatedAt",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "CondominiumId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_CondominiumId_DocumentId",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "CondominiumId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_CondominiumId_MeetingId",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "CondominiumId", "MeetingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_CondominiumId_PreviousMinutesId",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "CondominiumId", "PreviousMinutesId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_MeetingId",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "MeetingId" },
                unique: true,
                filter: "\"Status\" IN (0,1)");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_TenantId_MeetingId_Version",
                table: "MeetingMinutes",
                columns: new[] { "TenantId", "MeetingId", "Version" },
                unique: true);
            DocumentConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Document history is forward-only.");
    }
}
