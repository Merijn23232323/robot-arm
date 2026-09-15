using Microsoft.EntityFrameworkCore;
using RobotArm.Api.Models;

namespace RobotArm.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Robot> Robots { get; set; }
    public DbSet<RobotCommand> RobotCommands { get; set; }
}