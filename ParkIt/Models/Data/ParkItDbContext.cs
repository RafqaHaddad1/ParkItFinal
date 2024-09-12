using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data;

namespace ParkIt.Models.Data
{
    public class ParkItDbContext : DbContext
    {
        public ParkItDbContext(DbContextOptions<ParkItDbContext> options) : base(options)
        {
        }
        public DbSet<ParkIt.Models.Data.Employee> Employee { get; set; } = default!;
        public DbSet<ParkIt.Models.Data.Zone> Zone { get; set; } = default!;
        public DbSet<ParkIt.Models.Data.Transactions> Transactions { get; set; } = default!;
        public DbSet<Event> Event { get; set; }
        public DbSet<Subzone> Subzone { get; set; }
    }
}
