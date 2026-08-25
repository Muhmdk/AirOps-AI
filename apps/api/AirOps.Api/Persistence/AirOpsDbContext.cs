using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Disruptions;
using AirOps.Api.Modules.Flights;
using AirOps.Api.Modules.Operations;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Persistence;

public sealed class AirOpsDbContext(DbContextOptions<AirOpsDbContext> options) : DbContext(options)
{
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<OperationalEvent> OperationalEvents => Set<OperationalEvent>();
    public DbSet<SimulationClockState> SimulationClocks => Set<SimulationClockState>();
    public DbSet<Disruption> Disruptions => Set<Disruption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var flight = modelBuilder.Entity<Flight>();
        flight.ToTable("flights");
        flight.HasKey(item => item.Id);
        flight.Property(item => item.Id).HasColumnName("id").HasMaxLength(8);
        flight.Property(item => item.OriginCode).HasColumnName("origin_code").HasMaxLength(3);
        flight.Property(item => item.Origin).HasColumnName("origin").HasMaxLength(80);
        flight.Property(item => item.DestinationCode).HasColumnName("destination_code").HasMaxLength(3);
        flight.Property(item => item.Destination).HasColumnName("destination").HasMaxLength(80);
        flight.Property(item => item.ScheduledDeparture).HasColumnName("scheduled_departure");
        flight.Property(item => item.ScheduledArrival).HasColumnName("scheduled_arrival");
        flight.Property(item => item.AircraftRegistration).HasColumnName("aircraft_registration").HasMaxLength(12);
        flight.Property(item => item.AircraftType).HasColumnName("aircraft_type").HasMaxLength(60);
        flight.Property(item => item.Gate).HasColumnName("gate").HasMaxLength(8);
        flight.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        flight.Property(item => item.Risk).HasColumnName("risk");
        flight.Property(item => item.DelayMinutes).HasColumnName("delay_minutes");
        flight.Property(item => item.Passengers).HasColumnName("passengers");
        flight.Property(item => item.ConnectingPassengers).HasColumnName("connecting_passengers");
        flight.Property(item => item.RiskLabel).HasColumnName("risk_label").HasMaxLength(120);
        flight.Ignore(item => item.EstimatedDeparture);
        flight.Ignore(item => item.EstimatedArrival);
        flight.Ignore(item => item.Route);
        flight.HasIndex(item => item.ScheduledDeparture);
        flight.HasIndex(item => item.Status);
        flight.HasIndex(item => item.Risk);

        var airport = modelBuilder.Entity<Airport>();
        airport.ToTable("airports");
        airport.HasKey(item => item.Code);
        airport.Property(item => item.Code).HasColumnName("code").HasMaxLength(3);
        airport.Property(item => item.Name).HasColumnName("name").HasMaxLength(120);
        airport.Property(item => item.City).HasColumnName("city").HasMaxLength(80);
        airport.Property(item => item.Province).HasColumnName("province").HasMaxLength(2);
        airport.Property(item => item.Timezone).HasColumnName("timezone").HasMaxLength(8);
        airport.Property(item => item.Risk).HasColumnName("risk").HasConversion<string>().HasMaxLength(20);
        airport.Property(item => item.Health).HasColumnName("health");
        airport.Property(item => item.AverageDelay).HasColumnName("average_delay");
        airport.Property(item => item.Departures).HasColumnName("departures");
        airport.Property(item => item.Arrivals).HasColumnName("arrivals");
        airport.Property(item => item.AtRisk).HasColumnName("at_risk");
        airport.Property(item => item.GatesUsed).HasColumnName("gates_used");
        airport.Property(item => item.GatesTotal).HasColumnName("gates_total");
        airport.Property(item => item.Weather).HasColumnName("weather").HasMaxLength(80);
        airport.Property(item => item.Temperature).HasColumnName("temperature");
        airport.Property(item => item.Wind).HasColumnName("wind").HasMaxLength(40);
        airport.Property(item => item.Visibility).HasColumnName("visibility").HasMaxLength(20);
        airport.HasIndex(item => item.Risk);
        airport.HasIndex(item => item.Health);

        var aircraft = modelBuilder.Entity<Aircraft>();
        aircraft.ToTable("aircraft");
        aircraft.HasKey(item => item.Registration);
        aircraft.Property(item => item.Registration).HasColumnName("registration").HasMaxLength(12);
        aircraft.Property(item => item.Type).HasColumnName("type").HasMaxLength(60);
        aircraft.Property(item => item.Family).HasColumnName("family").HasConversion<string>().HasMaxLength(20);
        aircraft.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        aircraft.Property(item => item.Location).HasColumnName("location").HasMaxLength(3);
        aircraft.Property(item => item.NextFlight).HasColumnName("next_flight").HasMaxLength(12);
        aircraft.Property(item => item.NextDeparture).HasColumnName("next_departure");
        aircraft.Property(item => item.Utilization).HasColumnName("utilization");
        aircraft.Property(item => item.Cycles).HasColumnName("cycles");
        aircraft.Property(item => item.Hours).HasColumnName("hours").HasPrecision(5, 1);
        aircraft.Property(item => item.MaintenanceDue).HasColumnName("maintenance_due");
        aircraft.Property(item => item.Health).HasColumnName("health");
        aircraft.Property(item => item.Seats).HasColumnName("seats");
        aircraft.Property(item => item.RangeKilometres).HasColumnName("range_kilometres");
        aircraft.HasOne<Airport>().WithMany().HasForeignKey(item => item.Location)
            .OnDelete(DeleteBehavior.Restrict);
        aircraft.HasIndex(item => item.Status);
        aircraft.HasIndex(item => item.Family);
        aircraft.HasIndex(item => item.Location);

        var operationalEvent = modelBuilder.Entity<OperationalEvent>();
        operationalEvent.ToTable("operational_events");
        operationalEvent.HasKey(item => item.Id);
        operationalEvent.Property(item => item.Id).HasColumnName("id");
        operationalEvent.Property(item => item.OccurredAt).HasColumnName("occurred_at");
        operationalEvent.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        operationalEvent.Property(item => item.Title).HasColumnName("title").HasMaxLength(160);
        operationalEvent.Property(item => item.Detail).HasColumnName("detail").HasMaxLength(300);
        operationalEvent.Property(item => item.Accent).HasColumnName("accent").HasMaxLength(20);
        operationalEvent.Property(item => item.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(20);
        operationalEvent.Property(item => item.EntityType).HasColumnName("entity_type").HasMaxLength(20);
        operationalEvent.Property(item => item.EntityId).HasColumnName("entity_id").HasMaxLength(40);
        operationalEvent.Property(item => item.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20);
        operationalEvent.Property(item => item.EventKey).HasColumnName("event_key").HasMaxLength(160);
        operationalEvent.HasIndex(item => item.OccurredAt);
        operationalEvent.HasIndex(item => item.Severity);
        operationalEvent.HasIndex(item => item.Category);
        operationalEvent.HasIndex(item => item.EventKey).IsUnique();

        var clock = modelBuilder.Entity<SimulationClockState>();
        clock.ToTable("simulation_clock");
        clock.HasKey(item => item.Id);
        clock.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        clock.Property(item => item.CurrentTime).HasColumnName("current_time");
        clock.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        clock.Property(item => item.MinutesPerTick).HasColumnName("minutes_per_tick");
        clock.Property(item => item.UpdatedAt).HasColumnName("updated_at");

        var disruption = modelBuilder.Entity<Disruption>();
        disruption.ToTable("disruptions");
        disruption.HasKey(item => item.Id);
        disruption.Property(item => item.Id).HasColumnName("id").HasMaxLength(12);
        disruption.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40);
        disruption.Property(item => item.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(20);
        disruption.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        disruption.Property(item => item.AirportCode).HasColumnName("airport_code").HasMaxLength(3);
        disruption.Property(item => item.PrimaryFlightId).HasColumnName("primary_flight_id").HasMaxLength(8);
        disruption.Property(item => item.StartedAt).HasColumnName("started_at");
        disruption.Property(item => item.DurationMinutes).HasColumnName("duration_minutes");
        disruption.Property(item => item.Description).HasColumnName("description").HasMaxLength(300);
        disruption.Property(item => item.CreatedAt).HasColumnName("created_at");
        disruption.Property(item => item.ResolvedAt).HasColumnName("resolved_at");
        disruption.Property(item => item.AffectedFlights).HasColumnName("affected_flights");
        disruption.Property(item => item.AffectedPassengers).HasColumnName("affected_passengers");
        disruption.Property(item => item.MissedConnections).HasColumnName("missed_connections");
        disruption.Property(item => item.CrewAffected).HasColumnName("crew_affected");
        disruption.Property(item => item.GateConflicts).HasColumnName("gate_conflicts");
        disruption.Property(item => item.HotelRooms).HasColumnName("hotel_rooms");
        disruption.Property(item => item.MealVouchers).HasColumnName("meal_vouchers");
        disruption.Property(item => item.EstimatedCompensation).HasColumnName("estimated_compensation");
        disruption.Property(item => item.EstimatedOperationalCost).HasColumnName("estimated_operational_cost");
        disruption.Property(item => item.RecoveryMinutes).HasColumnName("recovery_minutes");
        disruption.HasOne<Airport>().WithMany().HasForeignKey(item => item.AirportCode)
            .OnDelete(DeleteBehavior.Restrict);
        disruption.HasOne<Flight>().WithMany().HasForeignKey(item => item.PrimaryFlightId)
            .OnDelete(DeleteBehavior.Restrict);
        disruption.HasIndex(item => item.Status);
        disruption.HasIndex(item => item.Severity);
        disruption.HasIndex(item => item.AirportCode);
        disruption.HasIndex(item => item.StartedAt);

        var impactedFlight = modelBuilder.Entity<ImpactedFlight>();
        impactedFlight.ToTable("disruption_flights");
        impactedFlight.HasKey(item => item.Id);
        impactedFlight.Property(item => item.Id).HasColumnName("id");
        impactedFlight.Property(item => item.DisruptionId).HasColumnName("disruption_id").HasMaxLength(12);
        impactedFlight.Property(item => item.Sequence).HasColumnName("sequence");
        impactedFlight.Property(item => item.FlightId).HasColumnName("flight_id").HasMaxLength(8);
        impactedFlight.Property(item => item.Route).HasColumnName("route").HasMaxLength(20);
        impactedFlight.Property(item => item.OriginalDelay).HasColumnName("original_delay");
        impactedFlight.Property(item => item.PropagatedDelay).HasColumnName("propagated_delay");
        impactedFlight.Property(item => item.Passengers).HasColumnName("passengers");
        impactedFlight.Property(item => item.MissedConnections).HasColumnName("missed_connections");
        impactedFlight.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(120);
        impactedFlight.HasOne<Disruption>().WithMany(item => item.Flights)
            .HasForeignKey(item => item.DisruptionId).OnDelete(DeleteBehavior.Cascade);
        impactedFlight.HasIndex(item => new { item.DisruptionId, item.Sequence }).IsUnique();

        var connection = modelBuilder.Entity<PassengerConnectionImpact>();
        connection.ToTable("disruption_connections");
        connection.HasKey(item => item.Id);
        connection.Property(item => item.Id).HasColumnName("id");
        connection.Property(item => item.DisruptionId).HasColumnName("disruption_id").HasMaxLength(12);
        connection.Property(item => item.Sequence).HasColumnName("sequence");
        connection.Property(item => item.InboundFlight).HasColumnName("inbound_flight").HasMaxLength(8);
        connection.Property(item => item.OutboundFlight).HasColumnName("outbound_flight").HasMaxLength(8);
        connection.Property(item => item.ConnectionAirport).HasColumnName("connection_airport").HasMaxLength(3);
        connection.Property(item => item.Passengers).HasColumnName("passengers");
        connection.Property(item => item.MinimumConnectionMinutes).HasColumnName("minimum_connection_minutes");
        connection.Property(item => item.AvailableConnectionMinutes).HasColumnName("available_connection_minutes");
        connection.Property(item => item.Status).HasColumnName("status").HasMaxLength(20);
        connection.HasOne<Disruption>().WithMany(item => item.Connections)
            .HasForeignKey(item => item.DisruptionId).OnDelete(DeleteBehavior.Cascade);
        connection.HasIndex(item => new { item.DisruptionId, item.Sequence }).IsUnique();

        var gateConflict = modelBuilder.Entity<GateConflictImpact>();
        gateConflict.ToTable("disruption_gate_conflicts");
        gateConflict.HasKey(item => item.Id);
        gateConflict.Property(item => item.Id).HasColumnName("id");
        gateConflict.Property(item => item.DisruptionId).HasColumnName("disruption_id").HasMaxLength(12);
        gateConflict.Property(item => item.Sequence).HasColumnName("sequence");
        gateConflict.Property(item => item.Airport).HasColumnName("airport").HasMaxLength(3);
        gateConflict.Property(item => item.Gate).HasColumnName("gate").HasMaxLength(8);
        gateConflict.Property(item => item.IncomingFlight).HasColumnName("incoming_flight").HasMaxLength(8);
        gateConflict.Property(item => item.OccupyingFlight).HasColumnName("occupying_flight").HasMaxLength(8);
        gateConflict.Property(item => item.OverlapMinutes).HasColumnName("overlap_minutes");
        gateConflict.Property(item => item.Severity).HasColumnName("severity").HasMaxLength(20);
        gateConflict.HasOne<Disruption>().WithMany(item => item.GateDetails)
            .HasForeignKey(item => item.DisruptionId).OnDelete(DeleteBehavior.Cascade);
        gateConflict.HasIndex(item => new { item.DisruptionId, item.Sequence }).IsUnique();

        var crew = modelBuilder.Entity<CrewDutyImpact>();
        crew.ToTable("disruption_crew_impacts");
        crew.HasKey(item => item.Id);
        crew.Property(item => item.Id).HasColumnName("id");
        crew.Property(item => item.DisruptionId).HasColumnName("disruption_id").HasMaxLength(12);
        crew.Property(item => item.Sequence).HasColumnName("sequence");
        crew.Property(item => item.CrewId).HasColumnName("crew_id").HasMaxLength(20);
        crew.Property(item => item.FlightId).HasColumnName("flight_id").HasMaxLength(8);
        crew.Property(item => item.Role).HasColumnName("role").HasMaxLength(40);
        crew.Property(item => item.ProjectedDutyMinutes).HasColumnName("projected_duty_minutes");
        crew.Property(item => item.LegalLimitMinutes).HasColumnName("legal_limit_minutes");
        crew.Property(item => item.RemainingMinutes).HasColumnName("remaining_minutes");
        crew.Property(item => item.Status).HasColumnName("status").HasMaxLength(20);
        crew.HasOne<Disruption>().WithMany(item => item.CrewDetails)
            .HasForeignKey(item => item.DisruptionId).OnDelete(DeleteBehavior.Cascade);
        crew.HasIndex(item => new { item.DisruptionId, item.Sequence }).IsUnique();
    }
}
