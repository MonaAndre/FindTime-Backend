using Microsoft.EntityFrameworkCore;

namespace FindTime.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Dina DbSets kommer här senare
    // public DbSet<User> Users { get; set; }
    // public DbSet<Group> Groups { get; set; }
    // public DbSet<Event> Events { get; set; }
}