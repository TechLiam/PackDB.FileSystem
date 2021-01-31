namespace PackDB.Core.Auditing
{
    public enum AuditType
    {
        Unknown = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        Undelete = 4,
        Rollback = 5,
    }
}