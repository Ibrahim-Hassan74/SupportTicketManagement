using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.Enums;

namespace SupportTicketManagement.Infrastructure.Data.Configuration
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasDefaultValue(TicketStatus.Open);

            builder.Property(t => t.Priority)
                .IsRequired()
                .HasDefaultValue(TicketPriority.Medium);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.UpdatedAt)
                .IsRequired();

            builder.Property(t => t.RowVersion)
                .IsRowVersion();

            // Relationships

            builder.HasOne(t => t.Customer)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.AssignedAgent)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes

            builder.HasIndex(t => t.CustomerId)
                .HasDatabaseName("IX_Tickets_CustomerId");

            builder.HasIndex(t => t.AssignedAgentId)
                .HasDatabaseName("IX_Tickets_AssignedAgentId")
                .HasFilter("[AssignedAgentId] IS NOT NULL");

            builder.HasIndex(t => t.Status)
                .HasDatabaseName("IX_Tickets_Status");

            builder.HasIndex(t => new { t.Status, t.Priority })
                .HasDatabaseName("IX_Tickets_Status_Priority");

            builder.HasIndex(t => t.CreatedAt)
                .HasDatabaseName("IX_Tickets_CreatedAt")
                .IsDescending();
        }
    }
}
