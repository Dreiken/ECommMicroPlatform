using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs.Role;
using UserService.Services;
using UserService.Constants;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleConstants.Admin)]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAllRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all roles");
            return StatusCode(500, "An error occurred while retrieving roles");
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoleDto>> GetRoleById(Guid id)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound($"Role with ID {id} not found");
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving role {RoleId}", id);
            return StatusCode(500, "An error occurred while retrieving the role");
        }
    }


    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        try
        {
            var (succeeded, errors) = await _roleService.CreateRoleAsync(dto);
            
            if (!succeeded)
            {
                return BadRequest(new { Errors = errors });
            }

            // Return a 201 Created response
            return CreatedAtAction(nameof(GetRoleById), new { id = Guid.Empty }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating role {RoleName}", dto.Name);
            return StatusCode(500, "An error occurred while creating the role");
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var (succeeded, errors) = await _roleService.UpdateRoleAsync(id, dto);
            
            if (!succeeded)
            {
                if (errors.Contains("Role not found"))
                {
                    return NotFound($"Role with ID {id} not found");
                }
                return BadRequest(new { Errors = errors });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating role {RoleId}", id);
            return StatusCode(500, "An error occurred while updating the role");
        }
    }


    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            var (succeeded, errors) = await _roleService.DeleteRoleAsync(id);
            
            if (!succeeded)
            {
                if (errors.Contains("Role not found"))
                {
                    return NotFound($"Role with ID {id} not found");
                }
                return BadRequest(new { Errors = errors });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting role {RoleId}", id);
            return StatusCode(500, "An error occurred while deleting the role");
        }
    }

    [HttpPost("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignRoleToUser(Guid userId, [FromBody] string roleName)
    {
        try
        {
            var (succeeded, errors) = await _roleService.AssignRoleToUserAsync(userId, roleName);
            
            if (!succeeded)
            {
                return BadRequest(new { Errors = errors });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning role {RoleName} to user {UserId}", 
                roleName, userId);
            return StatusCode(500, "An error occurred while assigning the role");
        }
    }
    
    [HttpGet("users/{userId:guid}/roles")]
    [ProducesResponseType(typeof(IList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<string>>> GetUserRoles(Guid userId)
    {
        try
        {
            var roles = await _roleService.GetUserRolesAsync(userId);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving roles for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user roles");
        }
    }
}