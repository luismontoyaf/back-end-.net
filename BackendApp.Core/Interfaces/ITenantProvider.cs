namespace Core.Interfaces
{
    public interface ITenantProvider
    {
        int GetTenantId();
        int GetUserId();
    }
}
