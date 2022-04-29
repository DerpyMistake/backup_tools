using System;

namespace BitEffects.BackupTools
{
    public class VaultInventory
    {
        public class ArchiveEntry
        {
            public string ArchiveId { get; set; }
            public string ArchiveDescription { get; set; }
            public DateTime CreationDate { get; set; }
            public int Size { get; set; }
            public string SHA256TreeHash { get; set; }

            private ArchiveMetaData _metaData = null;
            public ArchiveMetaData Metadata => _metaData = _metaData ?? ArchiveMetaData.FromDescription(this.ArchiveDescription);
        }

        public string VaultARN { get; set; }
        public DateTime InventoryDate { get; set; }
        public ArchiveEntry[] ArchiveList { get; set; }
        // {"VaultARN":"arn:aws:glacier:us-east-2:688411830953:vaults/home-lab-backup","InventoryDate":"1970-01-01T00:00:00Z","ArchiveList":[]}
    }
}
