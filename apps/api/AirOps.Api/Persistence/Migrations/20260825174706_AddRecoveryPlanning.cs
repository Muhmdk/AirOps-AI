using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recovery_plans",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    flights_affected = table.Column<string[]>(type: "text[]", nullable: false),
                    aircraft_affected = table.Column<string[]>(type: "text[]", nullable: false),
                    passengers_affected = table.Column<int>(type: "integer", nullable: false),
                    missed_connections = table.Column<int>(type: "integer", nullable: false),
                    expected_delay_minutes = table.Column<int>(type: "integer", nullable: false),
                    recovery_minutes = table.Column<int>(type: "integer", nullable: false),
                    estimated_cost = table.Column<int>(type: "integer", nullable: false),
                    operational_risk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    advantages = table.Column<string[]>(type: "text[]", nullable: false),
                    disadvantages = table.Column<string[]>(type: "text[]", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    recommended = table.Column<bool>(type: "boolean", nullable: false),
                    delay_score = table.Column<int>(type: "integer", nullable: false),
                    cost_score = table.Column<int>(type: "integer", nullable: false),
                    passenger_score = table.Column<int>(type: "integer", nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_recovery_plans_disruptions_disruption_id",
                        column: x => x.disruption_id,
                        principalTable: "disruptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recovery_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    disruption_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    supervisor_override = table.Column<bool>(type: "boolean", nullable: false),
                    delay_before = table.Column<int>(type: "integer", nullable: false),
                    delay_after = table.Column<int>(type: "integer", nullable: false),
                    cost_before = table.Column<int>(type: "integer", nullable: false),
                    cost_after = table.Column<int>(type: "integer", nullable: false),
                    missed_before = table.Column<int>(type: "integer", nullable: false),
                    missed_after = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_recovery_audit_entries_recovery_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "recovery_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recovery_audit_entries_disruption_id",
                table: "recovery_audit_entries",
                column: "disruption_id");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_audit_entries_plan_id",
                table: "recovery_audit_entries",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_audit_entries_timestamp",
                table: "recovery_audit_entries",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_plans_disruption_id_action",
                table: "recovery_plans",
                columns: new[] { "disruption_id", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recovery_plans_score",
                table: "recovery_plans",
                column: "score");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_plans_status",
                table: "recovery_plans",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery_audit_entries");

            migrationBuilder.DropTable(
                name: "recovery_plans");
        }
    }
}
