using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportAndAircraftOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    province = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    timezone = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    risk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    health = table.Column<int>(type: "integer", nullable: false),
                    average_delay = table.Column<int>(type: "integer", nullable: false),
                    departures = table.Column<int>(type: "integer", nullable: false),
                    arrivals = table.Column<int>(type: "integer", nullable: false),
                    at_risk = table.Column<int>(type: "integer", nullable: false),
                    gates_used = table.Column<int>(type: "integer", nullable: false),
                    gates_total = table.Column<int>(type: "integer", nullable: false),
                    weather = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    temperature = table.Column<int>(type: "integer", nullable: false),
                    wind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airports", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "aircraft",
                columns: table => new
                {
                    registration = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    family = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    next_flight = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    next_departure = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    utilization = table.Column<int>(type: "integer", nullable: false),
                    cycles = table.Column<int>(type: "integer", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    maintenance_due = table.Column<int>(type: "integer", nullable: false),
                    health = table.Column<int>(type: "integer", nullable: false),
                    seats = table.Column<int>(type: "integer", nullable: false),
                    range_kilometres = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aircraft", x => x.registration);
                    table.ForeignKey(
                        name: "FK_aircraft_airports_location",
                        column: x => x.location,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aircraft_family",
                table: "aircraft",
                column: "family");

            migrationBuilder.CreateIndex(
                name: "IX_aircraft_location",
                table: "aircraft",
                column: "location");

            migrationBuilder.CreateIndex(
                name: "IX_aircraft_status",
                table: "aircraft",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_airports_health",
                table: "airports",
                column: "health");

            migrationBuilder.CreateIndex(
                name: "IX_airports_risk",
                table: "airports",
                column: "risk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aircraft");

            migrationBuilder.DropTable(
                name: "airports");
        }
    }
}
