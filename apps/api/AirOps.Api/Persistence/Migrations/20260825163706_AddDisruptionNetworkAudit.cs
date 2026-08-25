using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisruptionNetworkAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disruption_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_audit_entries_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disruption_network_mutations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    field = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    before_value = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    after_value = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_network_mutations", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_network_mutations_disruption_audit_entries_audit~",
                        column: x => x.audit_entry_id,
                        principalTable: "disruption_audit_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disruption_audit_entries_disruption_id",
                table: "disruption_audit_entries",
                column: "disruption_id");

            migrationBuilder.CreateIndex(
                name: "IX_disruption_audit_entries_timestamp",
                table: "disruption_audit_entries",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_disruption_network_mutations_audit_entry_id",
                table: "disruption_network_mutations",
                column: "audit_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_disruption_network_mutations_entity_type_entity_id",
                table: "disruption_network_mutations",
                columns: new[] { "entity_type", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disruption_network_mutations");

            migrationBuilder.DropTable(
                name: "disruption_audit_entries");
        }
    }
}
