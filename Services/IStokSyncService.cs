using System.Threading.Tasks;

namespace itam.Services
{
    public interface IStokSyncService
    {
        Task<int> SyncBarangAsync(int barangId);
        Task<int> SyncAllBarangAsync();
    }
}
