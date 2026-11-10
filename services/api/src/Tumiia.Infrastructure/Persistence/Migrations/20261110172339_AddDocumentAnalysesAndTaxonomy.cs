using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tumiia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAnalysesAndTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "latest_analysis_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pipeline_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    analysed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detected_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    content_type_detected = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    proposed_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proposed_category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    runner_up_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    classification_confidence = table.Column<double>(type: "double precision", nullable: false),
                    band = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    signals = table.Column<string>(type: "jsonb", nullable: false),
                    fields = table.Column<string>(type: "jsonb", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    warnings = table.Column<string>(type: "jsonb", nullable: false),
                    analyser = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_analyses_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "taxonomy_categories",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    definition = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxonomy_categories", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_analyses_document_id_pipeline_version",
                table: "document_analyses",
                columns: new[] { "document_id", "pipeline_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_analyses");

            migrationBuilder.DropTable(
                name: "taxonomy_categories");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "latest_analysis_id",
                table: "documents");
        }
    }
}
