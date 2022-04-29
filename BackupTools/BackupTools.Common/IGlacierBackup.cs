using System.IO;
using System.Threading.Tasks;

namespace BitEffects.BackupTools
{
    public interface IGlacierBackup
    {
        Task<string> InitiateMultipartUpload();
        Task<string> CompleteMultipartUpload();
        Task UploadPart(Stream stream);
    }
}
