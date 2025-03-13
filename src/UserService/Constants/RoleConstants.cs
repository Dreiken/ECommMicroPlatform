namespace UserService.Constants;
public static class RoleConstants
{
    public const string Admin = nameof(Admin);
    public const string Boss = nameof(Boss);
    public const string Employee = nameof(Employee);
    public const string User = nameof(User);

    public static readonly IReadOnlyList<string> AllRoles = new[] { Admin, Boss, Employee, User };  
}