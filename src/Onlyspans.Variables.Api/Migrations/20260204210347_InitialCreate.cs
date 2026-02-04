using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onlyspans.Variables.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VariableSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectVariableSetLinks",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariableSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectVariableSetLinks", x => new { x.ProjectId, x.VariableSetId });
                    table.ForeignKey(
                        name: "FK_ProjectVariableSetLinks_VariableSets_VariableSetId",
                        column: x => x.VariableSetId,
                        principalTable: "VariableSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Variables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    VariableSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variables_VariableSets_VariableSetId",
                        column: x => x.VariableSetId,
                        principalTable: "VariableSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVariableSetLinks_ProjectId",
                table: "ProjectVariableSetLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVariableSetLinks_VariableSetId",
                table: "ProjectVariableSetLinks",
                column: "VariableSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_EnvironmentId",
                table: "Variables",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_ProjectId",
                table: "Variables",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_ProjectId_Key_EnvironmentId",
                table: "Variables",
                columns: new[] { "ProjectId", "Key", "EnvironmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_VariableSetId",
                table: "Variables",
                column: "VariableSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_VariableSetId_Key_EnvironmentId",
                table: "Variables",
                columns: new[] { "VariableSetId", "Key", "EnvironmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableSets_Name",
                table: "VariableSets",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectVariableSetLinks");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "VariableSets");
        }
    }
}
