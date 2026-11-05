using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tumiia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "document_id",
                table: "audit_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_asp_net_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type_claimed = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type_detected = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    extension_matches = table.Column<bool>(type: "boolean", nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    capture_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    consequence_level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    needs_review = table.Column<bool>(type: "boolean", nullable: false),
                    correction_flag = table.Column<bool>(type: "boolean", nullable: false),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    document_type_confirmed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject_confirmed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    confirmed_fields = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_asp_net_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documents_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_project_members_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_document_id",
                table: "audit_events",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_id_sha256",
                table: "documents",
                columns: new[] { "project_id", "sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_id_status",
                table: "documents",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_storage_key",
                table: "documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by",
                table: "documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user_id",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_owner_user_id",
                table: "projects",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropIndex(
                name: "ix_audit_events_document_id",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "document_id",
                table: "audit_events");
        }
    }
}
