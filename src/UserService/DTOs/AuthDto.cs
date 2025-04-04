namespace UserService.DTOs.Auth;

public record RegisterDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FullName { get; init; } = default!;
}

public record LoginDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}

public record AuthResponseDto
{
    public string Token { get; init; } = default!;
    public string Email { get; init; } = default!;
    public IList<string> Roles { get; init; } = new List<string>();
}