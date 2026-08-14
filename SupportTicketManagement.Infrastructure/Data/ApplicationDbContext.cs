using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using System.Reflection;

namespace SupportTicketManagement.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public virtual DbSet<Ticket> Tickets { get; set;  }
        public virtual DbSet<TicketComment> TicketComments { get; set; }
        public virtual DbSet<TicketActivity> TicketActivities { get; set; }
        public virtual DbSet<TimeEntry> TimeEntries { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}

