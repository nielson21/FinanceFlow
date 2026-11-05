namespace FinanceFlow.Constants;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Approver = "Approver";
    public const string Requestor = "Requestor";
    public static IReadOnlyList<string> All { get; } =
        [Admin, Approver, Requestor];
}