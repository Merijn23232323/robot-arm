using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobotArm.Api.Data;
using RobotArm.Api.Models;

namespace RobotArm.Api.Controllers;

[ApiController]
[Route("api/commands")]
public class RobotCommandsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RobotCommandsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/commands
    [EndpointSummary("Haal alle commands op")]
    [EndpointDescription("Geeft alle robotcommando's terug die in het systeem zijn opgeslagen.")]
    [ProducesResponseType(typeof(IEnumerable<RobotCommand>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RobotCommand>>> GetCommands()
    {
        var commands = await _context.RobotCommands
            .ToListAsync();

        return Ok(commands);
    }

    // GET: api/commands/5
    [EndpointSummary("Haal een command op")]
    [EndpointDescription("Geeft één robotcommando terug op basis van het command-ID.")]
    [ProducesResponseType(typeof(RobotCommand), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<RobotCommand>> GetCommand(int id)
    {
        var command = await _context.RobotCommands
            .FindAsync(id);

        if (command == null)
        {
            return NotFound(new
            {
                message = $"Command met id {id} is niet gevonden."
            });
        }

        return Ok(command);
    }

    // POST: api/commands
    [EndpointSummary("Maak een command aan")]
    [EndpointDescription("Maakt een nieuw commando aan voor een bestaande robot.")]
    [ProducesResponseType(typeof(RobotCommand), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<ActionResult<RobotCommand>> CreateCommand(
        RobotCommand command)
    {
        // Controleren of de robot bestaat
        var robotExists = await _context.Robots
            .AnyAsync(r => r.Id == command.RobotId);

        if (!robotExists)
        {
            return BadRequest(new
            {
                message = $"Robot met id {command.RobotId} bestaat niet."
            });
        }

        _context.RobotCommands.Add(command);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCommand),
            new { id = command.Id },
            command
        );
    }

    // PUT: api/commands/5
    [EndpointSummary("Werk een command bij")]
    [EndpointDescription("Wijzigt een bestaand robotcommando.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommand(
        int id,
        RobotCommand updatedCommand)
    {
        var command = await _context.RobotCommands.FindAsync(id);

        if (command == null)
        {
            return NotFound(new
            {
                message = $"Command met id {id} is niet gevonden."
            });
        }

        var robotExists = await _context.Robots
            .AnyAsync(r => r.Id == updatedCommand.RobotId);

        if (!robotExists)
        {
            return BadRequest(new
            {
                message = $"Robot met id {updatedCommand.RobotId} bestaat niet."
            });
        }

        command.RobotId = updatedCommand.RobotId;
        command.Servo = updatedCommand.Servo;
        command.Angle = updatedCommand.Angle;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/commands/5
    [EndpointSummary("Verwijder een command")]
    [EndpointDescription("Verwijdert een robotcommando op basis van het command-ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommand(int id)
    {
        var command = await _context.RobotCommands.FindAsync(id);

        if (command == null)
        {
            return NotFound(new
            {
                message = $"Command met id {id} is niet gevonden."
            });
        }

        _context.RobotCommands.Remove(command);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}