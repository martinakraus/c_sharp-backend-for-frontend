using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPIApplication.Controllers;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[Route("")]
[ApiController]
public class ApiController : ControllerBase
{
    private static List<User> _users = new List<User>
    {
        new User { Id = 1, Name = "Max Mustermann", Email = "max@example.com" },
        new User { Id = 2, Name = "Erika Musterfrau", Email = "erika@example.com" },
        new User { Id = 3, Name = "Hans Schmidt", Email = "hans@example.com" }
    };
    private static int _nextId = 4;
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger)
    {
        _logger = logger;
    }

    [HttpGet("users")]
    [Authorize("users:read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetUsers()
    {
        _logger.LogInformation("Users wurden erfolgreich geladen. Anzahl: {Count}", _users.Count);
        return Ok(_users);
    }

    [HttpGet("users/{id}")]
    [Authorize("users:read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetUserById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            _logger.LogWarning("User mit ID {UserId} nicht gefunden", id);
            return NotFound(new { Message = $"User with ID {id} not found." });
        }
        _logger.LogInformation("User erfolgreich geladen: {UserName} (ID: {UserId})", user.Name, user.Id);
        return Ok(user);
    }

    [HttpPost("users")]
    [Authorize("users:write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            _logger.LogWarning("User anlegen fehlgeschlagen: Name oder Email fehlt");
            return BadRequest(new { Message = "Name and Email are required." });
        }

        var newUser = new User
        {
            Id = _nextId++,
            Name = request.Name,
            Email = request.Email
        };

        _users.Add(newUser);
        _logger.LogInformation("User erfolgreich angelegt: {UserName} (ID: {UserId}, Email: {Email})", 
            newUser.Name, newUser.Id, newUser.Email);
        return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
    }
}