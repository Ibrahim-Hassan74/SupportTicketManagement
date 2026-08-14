using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketManagement.Core.Domain.Entities;

namespace SupportTicketManagement.Infrastructure.Data.Configuration
{
    public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
    {
        public void Configure(EntityTypeBuilder<TimeEntry> builder)
        {
            builder.HasKey(te => te.Id);

            builder.Property(te => te.WorkDate)
                .IsRequired();

            builder.Property(te => te.DurationMinutes)
                .IsRequired();

            builder.Property(te => te.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(te => te.CreatedAt)
                .IsRequired();

            // Relationships

            builder.HasOne(te => te.Ticket)
                .WithMany(t => t.TimeEntries)
                .HasForeignKey(te => te.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(te => te.Agent)
                .WithMany(u => u.TimeEntries)
                .HasForeignKey(te => te.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes

            builder.HasIndex(te => te.TicketId)
                .HasDatabaseName("IX_TimeEntries_TicketId");

            // Check constraint: duration must be between 1 and 1440 minutes (24 hours)
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_TimeEntries_DurationMinutes",
                "[DurationMinutes] > 0 AND [DurationMinutes] <= 1440"));
        }
    }
}
