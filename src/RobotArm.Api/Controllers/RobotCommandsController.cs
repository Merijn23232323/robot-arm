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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RobotCommand>>> GetCommands()
    {
        var commands = await _context.RobotCommands
            .ToListAsync();

        return Ok(commands);
    }

    // GET: api/commands/5
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