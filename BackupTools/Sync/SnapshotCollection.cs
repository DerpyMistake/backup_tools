using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Sync
{
    public class SnapshotCollection
    {
        public string ArchivePath { get; set; }
        public List<Snapshot> Snapshots { get; set; } = new List<Snapshot>();

        /// <summary>
        /// Load a collection from a compressed snapshot source
        /// </summary>
        /// <param name="snapshotPath"></param>
        /// <returns></returns>
        async static public Task<SnapshotCollection> Load(string snapshotPath, string archivePath)
        {
            if (File.Exists(snapshotPath))
            {
                using (Stream fstream = new FileStream(snapshotPath, FileMode.Open, FileAccess.Read))
                using (GZipStream gzStream = new GZipStream(fstream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzStream))
                {
                    string json = await reader.ReadToEndAsync();
                    return json.Deserialize<SnapshotCollection>() ?? new SnapshotCollection();
                }
            }

            return new SnapshotCollection() { ArchivePath = archivePath };
        }

        /// <summary>
        /// Output the gzipped collection to a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        async public Task Save(string path)
        {
            var bytes = Encoding.UTF8.GetBytes(this.Serialize(true));

            using (Stream fstream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (GZipStream gzStream = new GZipStream(fstream, CompressionMode.Compress))
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                await ms.CopyToAsync(gzStream);
            }
        }

        /// <summary>
        /// Patch all of the snapshots up to and including the specified end date
        /// </summary>
        /// <param name="end"></param>
        /// <returns>A new base snapshot</returns>
        public Snapshot BuildSnapshot(DateTime end)
        {
            var orderedSnapshots = this.Snapshots.OrderBy(s => s.Date).ToArray();
            Snapshot res = orderedSnapshots.FirstOrDefault() ?? new Snapshot();

            // Build up our snapshot
            foreach (var snap in orderedSnapshots.Where(s => s.Date <= end))
            {
                res = res.Patch(snap);
            }

            return res;
        }
    }
}
