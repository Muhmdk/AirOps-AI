using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisruptionImpactManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disruptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    airport_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    primary_flight_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    affected_flights = table.Column<int>(type: "integer", nullable: false),
                    affected_passengers = table.Column<int>(type: "integer", nullable: false),
                    missed_connections = table.Column<int>(type: "integer", nullable: false),
                    crew_affected = table.Column<int>(type: "integer", nullable: false),
                    gate_conflicts = table.Column<int>(type: "integer", nullable: false),
                    hotel_rooms = table.Column<int>(type: "integer", nullable: false),
                    meal_vouchers = table.Column<int>(type: "integer", nullable: false),
                    estimated_compensation = table.Column<int>(type: "integer", nullable: false),
                    estimated_operational_cost = table.Column<int>(type: "integer", nullable: false),
                    recovery_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruptions_airports_airport_code",
                        column: x => x.airport_code,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_disruptions_flights_primary_flight_id",
                        column: x => x.primary_flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disruption_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    inbound_flight = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    outbound_flight = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    connection_airport = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    passengers = table.Column<int>(type: "integer", nullable: false),
                    minimum_connection_minutes = table.Column<int>(type: "integer", nullable: false),
                    available_connection_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_connections", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_connections_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disruption_crew_impacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    crew_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    flight_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    projected_duty_minutes = table.Column<int>(type: "integer", nullable: false),
                    legal_limit_minutes = table.Column<int>(type: "integer", nullable: false),
                    remaining_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_crew_impacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_crew_impacts_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disruption_flights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    flight_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    route = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    original_delay = table.Column<int>(type: "integer", nullable: false),
                    propagated_delay = table.Column<int>(type: "integer", nullable: false),
                    passengers = table.Column<int>(type: "integer", nullable: false),
                    missed_connections = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_flights", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_flights_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disruption_gate_conflicts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    airport = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    gate = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    incoming_flight = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    occupying_flight = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    overlap_minutes = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disruption_gate_conflicts", x => x.id);
                    table.ForeignKey(
                        name: "FK_disruption_gate_conflicts_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disruption_connections_disruption_id_sequence",
                table: "disruption_connections",
                columns: new[] { "disruption_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disruption_crew_impacts_disruption_id_sequence",
                table: "disruption_crew_impacts",
                columns: new[] { "disruption_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disruption_flights_disruption_id_sequence",
                table: "disruption_flights",
                columns: new[] { "disruption_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disruption_gate_conflicts_disruption_id_sequence",
                table: "disruption_gate_conflicts",
                columns: new[] { "disruption_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disruptions_airport_code",
                table: "disruptions",
                column: "airport_code");

            migrationBuilder.CreateIndex(
                name: "IX_disruptions_primary_flight_id",
                table: "disruptions",
                column: "primary_flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_disruptions_severity",
                table: "disruptions",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_disruptions_started_at",
                table: "disruptions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_disruptions_status",
                table: "disruptions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disruption_connections");

            migrationBuilder.DropTable(
                name: "disruption_crew_impacts");

            migrationBuilder.DropTable(
                name: "disruption_flights");

            migrationBuilder.DropTable(
                name: "disruption_gate_conflicts");

            migrationBuilder.DropTable(
                name: "disruptions");
        }
    }
}
