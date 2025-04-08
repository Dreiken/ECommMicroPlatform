namespace UserService.DTOs.Role;

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}
public record CreateRoleDto
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}
public record UpdateRoleDto
{
    public string? Description { get; init; }
}
public record AssignRoleDto
{
    public string Name { get; set; } = string.Empty;
}