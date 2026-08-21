using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Flights;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Persistence;

public sealed class AirOpsDbContext(DbContextOptions<AirOpsDbContext> options) : DbContext(options)
{
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();

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
    }
}
