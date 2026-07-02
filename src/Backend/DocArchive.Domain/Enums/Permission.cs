namespace DocArchive.Domain.Enums;

[Flags]
public enum Permission
{
    None = 0,
    View = 1,
    Download = 2,
    Upload = 4,
    Delete = 8,
    ManageUsers = 16
}
