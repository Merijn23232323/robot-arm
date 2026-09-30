using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RobotArm.Api.Data;
using RobotArm.Api.Models;

namespace RobotArm.Api.Tests;

public sealed class RobotArmApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("RobotArmIntegrationTests"));
        });
    }

    public int SeedRobot()
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        database.Database.EnsureCreated();

        var robot = database.Robots.FirstOrDefault();
        if (robot is null)
        {
            robot = new Robot
            {
                Name = "Test Robot",
                IsOnline = true,
                UserId = 1
            };
            database.Robots.Add(robot);
            database.SaveChanges();
        }

        return robot.Id;
    }
}