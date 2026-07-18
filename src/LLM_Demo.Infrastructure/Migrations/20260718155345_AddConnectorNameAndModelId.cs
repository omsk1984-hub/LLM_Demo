using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLM_Demo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectorNameAndModelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "ConnectorName",
                schema: "llm_demo",
                table: "Agents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                schema: "llm_demo",
                table: "Agents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectorName",
                schema: "llm_demo",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ModelId",
                schema: "llm_demo",
                table: "Agents");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");
        }
    }
}
