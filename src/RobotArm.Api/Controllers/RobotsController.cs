using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobotArm.Api.Data;
using RobotArm.Api.Models;

namespace RobotArm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RobotsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/robots
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Robot>>> GetRobots()
    {
        var robots = await _context.Robots.ToListAsync();

        return Ok(robots);
    }

    // GET: api/robots/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Robot>> GetRobot(int id)
    {
        var robot = await _context.Robots.FindAsync(id);

        if (robot == null)
        {
            return NotFound(new
            {
                message = $"Robot met id {id} is niet gevonden."
            });
        }

        return Ok(robot);
    }

    // POST: api/robots
    [HttpPost]
    public async Task<ActionResult<Robot>> CreateRobot(Robot robot)
    {
        if (string.IsNullOrWhiteSpace(robot.Name))
        {
            return BadRequest(new
            {
                message = "De naam van de robot is verplicht."
            });
        }

        _context.Robots.Add(robot);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRobot),
            new { id = robot.Id },
            robot
        );
    }

    // PUT: api/robots/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRobot(int id, Robot robot)
    {
        if (id != robot.Id)
        {
            return BadRequest(new
            {
                message = "Het ID in de URL komt niet overeen met het robot ID."
            });
        }

        if (string.IsNullOrWhiteSpace(robot.Name))
        {
            return BadRequest(new
            {
                message = "De naam van de robot is verplicht."
            });
        }

        var existingRobot = await _context.Robots.FindAsync(id);

        if (existingRobot == null)
        {
            return NotFound(new
            {
                message = $"Robot met id {id} is niet gevonden."
            });
        }

        existingRobot.Name = robot.Name;
        existingRobot.IsOnline = robot.IsOnline;

        await _context.SaveChangesAsync();

        return Ok(existingRobot);
    }

    // DELETE: api/robots/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRobot(int id)
    {
        var robot = await _context.Robots.FindAsync(id);

        if (robot == null)
        {
            return NotFound(new
            {
                message = $"Robot met id {id} is niet gevonden."
            });
        }

        _context.Robots.Remove(robot);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Robot met id {id} is verwijderd."
        });
    }
}