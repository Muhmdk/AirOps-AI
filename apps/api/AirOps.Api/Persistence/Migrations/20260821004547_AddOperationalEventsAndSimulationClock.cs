using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalEventsAndSimulationClock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operational_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    detail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    accent = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulation_clock",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    current_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minutes_per_tick = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_clock", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_operational_events_category",
                table: "operational_events",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_operational_events_event_key",
                table: "operational_events",
                column: "event_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operational_events_occurred_at",
                table: "operational_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_operational_events_severity",
                table: "operational_events",
                column: "severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operational_events");

            migrationBuilder.DropTable(
                name: "simulation_clock");
        }
    }
}
