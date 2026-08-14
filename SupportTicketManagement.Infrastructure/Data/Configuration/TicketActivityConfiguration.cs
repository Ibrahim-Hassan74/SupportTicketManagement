using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketManagement.Core.Domain.Entities;

namespace SupportTicketManagement.Infrastructure.Data.Configuration
{
    public class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
    {
        public void Configure(EntityTypeBuilder<TicketActivity> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Type)
                .IsRequired();

            builder.Property(a => a.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.OldValue)
                .HasMaxLength(200);

            builder.Property(a => a.NewValue)
                .HasMaxLength(200);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            // Relationships

            builder.HasOne(a => a.Ticket)
                .WithMany(t => t.Activities)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes — composite for efficient timeline queries ordered by date

            builder.HasIndex(a => new { a.TicketId, a.CreatedAt })
                .HasDatabaseName("IX_TicketActivities_TicketId_CreatedAt");
        }
    }
}
