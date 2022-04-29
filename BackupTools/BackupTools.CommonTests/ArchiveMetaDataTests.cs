using NUnit.Framework;
using BitEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Tests
{
    [TestFixture]
    public class ArchiveMetaDataTests
    {
        [Test]
        public void DeserializedObjectMatchesOriginal()
        {
            ArchiveMetaData origTestData = new ArchiveMetaData("/path/to/file");
            origTestData.Tags.Add(ArchiveTags.Snapshot);

            string description = origTestData.ToDescription();
            ArchiveMetaData deserializedTestData = ArchiveMetaData.FromDescription(description);

            Assert.AreEqual(origTestData.Name, deserializedTestData.Name);
            Assert.AreEqual(origTestData.Path, deserializedTestData.Path);
            Assert.AreEqual(origTestData.Created, deserializedTestData.Created);
            Assert.AreEqual(string.Join(',', origTestData.Tags), string.Join(',', deserializedTestData.Tags));
        }

        [Test]
        public void DeserializedObjectMatchesOriginalWithName()
        {
            ArchiveMetaData origTestData = new ArchiveMetaData("name", "/path/to/file");
            origTestData.Tags.Add(ArchiveTags.Snapshot);

            string description = origTestData.ToDescription();
            ArchiveMetaData deserializedTestData = ArchiveMetaData.FromDescription(description);

            Assert.AreEqual(origTestData.Name, deserializedTestData.Name);
            Assert.AreEqual(origTestData.Path, deserializedTestData.Path);
            Assert.AreEqual(origTestData.Created, deserializedTestData.Created);
            Assert.AreEqual(string.Join(',', origTestData.Tags), string.Join(',', deserializedTestData.Tags));
        }
    }
}