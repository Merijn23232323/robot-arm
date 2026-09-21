using Microsoft.AspNetCore.Authorization;
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
    [EndpointSummary("Haal alle robots op")]
    [EndpointDescription("Geeft alle robots terug die in het systeem zijn geregistreerd.")]
    [ProducesResponseType(typeof(IEnumerable<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Robot>>> GetRobots()
    {
        var robots = await _context.Robots.ToListAsync();

        return Ok(robots);
    }

    // GET: api/robots/5
    [EndpointSummary("Haal een robot op")]
    [EndpointDescription("Geeft een robot terug op basis van het robot-ID.")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
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
    [EndpointSummary("Maak een robot aan")]
    [EndpointDescription("Registreert een nieuwe robot in het systeem. Alleen een Admin mag dit uitvoeren.")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Roles = "Admin")]
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
    [EndpointSummary("Werk een robot bij")]
    [EndpointDescription("Wijzigt de gegevens van een bestaande robot. Alleen een Admin mag dit uitvoeren.")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin")]
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
    [EndpointSummary("Verwijder een robot")]
    [EndpointDescription("Verwijdert een robot op basis van het robot-ID. Alleen een Admin mag dit uitvoeren.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin")]
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