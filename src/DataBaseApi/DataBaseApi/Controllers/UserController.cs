using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBaseApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    [HttpGet("list-users")]
    public async Task<IActionResult> ListUsers()
    {
        try
        {
            var users = new List<object>();
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            await foreach (var user in pagedEnumerable)
            {
                users.Add(new { uid = user.Uid, email = user.Email });
            }
            return Ok(new { users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto dto)
    {
        try
        {
            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
            {
                Email = dto.Email,
                Password = dto.Password
            });

            // Pose le custom claim admin:true immédiatement
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                userRecord.Uid,
                new Dictionary<string, object> { { "admin", true } }
            );

            // Génère un lien de réinitialisation pour l'envoyer au nouvel admin
            var resetLink = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(dto.Email);

            return Ok(new { uid = userRecord.Uid, resetLink });
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "Firebase error creating user");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        try
        {
            var resetLink = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(dto.Email);
            return Ok(new { resetLink });
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "Firebase error generating reset link");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reset link");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("delete-user")]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequestDto dto)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(dto.Uid);
            return Ok(new { message = "User deleted" });
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "Firebase error deleting user");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("set-admin-claim")]
    public async Task<IActionResult> SetAdminClaim([FromBody] SetAdminClaimRequestDto dto)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                dto.Uid,
                new Dictionary<string, object> { { "admin", true } }
            );
            return Ok(new { message = "Admin claim set" });
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError(ex, "Firebase error setting admin claim");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting admin claim");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
