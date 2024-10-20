﻿using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data;

namespace ParkIt.Models.Data
{
    public class ParkItDbContext : DbContext
    {
        public ParkItDbContext(DbContextOptions<ParkItDbContext> options) : base(options)
        {
        }
        public DbSet<Employee> Employee { get; set; } = default!;
        public DbSet<Zone> Zone { get; set; } = default!;
        public DbSet<Transactions> Transactions { get; set; } = default!;
        public DbSet<Event> Event { get; set; }
        public DbSet<Subzone> Subzone { get; set; }
        public DbSet<Admin> Admin { get; set; }
    }
}
