using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "passenger_journeys",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    booking_reference = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    lead_passenger = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    loyalty_tier = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    current_flight_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    connecting_flight_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    origin_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    connection_airport = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    destination_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    minimum_connection_minutes = table.Column<int>(type: "integer", nullable: false),
                    available_connection_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    special_services = table.Column<string[]>(type: "text[]", nullable: false),
                    alternative_flights = table.Column<string[]>(type: "text[]", nullable: false),
                    selected_alternative_flight = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    estimated_care_cost = table.Column<int>(type: "integer", nullable: false),
                    rebooking_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_journeys", x => x.id);
                    table.ForeignKey(
                        name: "FK_passenger_journeys_flights_current_flight_id",
                        column: x => x.current_flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_passenger_journeys_booking_reference",
                table: "passenger_journeys",
                column: "booking_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_passenger_journeys_current_flight_id",
                table: "passenger_journeys",
                column: "current_flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_journeys_risk_score",
                table: "passenger_journeys",
                column: "risk_score");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_journeys_status",
                table: "passenger_journeys",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passenger_journeys");
        }
    }
}
