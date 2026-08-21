using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    origin_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    origin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    destination_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    destination = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    scheduled_departure = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scheduled_arrival = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aircraft_registration = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    aircraft_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    gate = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk = table.Column<int>(type: "integer", nullable: false),
                    delay_minutes = table.Column<int>(type: "integer", nullable: false),
                    passengers = table.Column<int>(type: "integer", nullable: false),
                    connecting_passengers = table.Column<int>(type: "integer", nullable: false),
                    risk_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flights", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_flights_risk",
                table: "flights",
                column: "risk");

            migrationBuilder.CreateIndex(
                name: "IX_flights_scheduled_departure",
                table: "flights",
                column: "scheduled_departure");

            migrationBuilder.CreateIndex(
                name: "IX_flights_status",
                table: "flights",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flights");
        }
    }
}
