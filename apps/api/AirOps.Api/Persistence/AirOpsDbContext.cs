using AirOps.Api.Modules.Flights;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Persistence;

public sealed class AirOpsDbContext(DbContextOptions<AirOpsDbContext> options) : DbContext(options)
{
    public DbSet<Flight> Flights => Set<Flight>();

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
    }
}
