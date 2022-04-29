using NUnit.Framework;
using BitEffects.BackupTools.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BitEffects.BackupTools.Sync.Snapshot;

namespace BitEffects.BackupTools.Sync.Tests
{
    [TestFixture]
    public class SnapshotTests
    {
        DateTime oldDate;
        DateTime newDate;
        Snapshot baseSnapshot;
        Snapshot patchSnapshot;
        Snapshot syncSnapshot;

        [SetUp]
        public void SetUp()
        {
            oldDate = DateTime.UtcNow;
            newDate = oldDate.AddDays(1);

            baseSnapshot = new Snapshot()
            {
                Entries = new List<Snapshot.Entry>()
                {
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F1", Path = "file1" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F2", Path = "file2" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F3", Path = "file3" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F4", Path = "file4" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F6", Path = "file6" },
                }
            };

            patchSnapshot = new Snapshot()
            {
                Entries = new List<Snapshot.Entry>()
                {
                    new Snapshot.Entry() { LastModified=newDate, Hash="F1", Path = "file1", Action = Snapshot.EntryAction.Remove },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F2", Path = "file2", Action = Snapshot.EntryAction.Add },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F3", Path = "file3", Action = Snapshot.EntryAction.None },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F5", Path = "file5", Action = Snapshot.EntryAction.Add },
                }
            };

            syncSnapshot = new Snapshot()
            {
                Entries = new List<Snapshot.Entry>()
                {
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F2", Path = "file2" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F3", Path = "file3" },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F4", Path = "file4" },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F5", Path = "file5" },
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F6", Path = "file7" },
                }
            };
        }

        [Test]
        public void SynchronizeDoesNotModifySources()
        {
            var originalBase = baseSnapshot.Serialize();
            var originalSync = syncSnapshot.Serialize();
            _ = baseSnapshot.Syncronize(syncSnapshot);

            Assert.AreEqual(originalBase, baseSnapshot.Serialize());
            Assert.AreEqual(originalSync, syncSnapshot.Serialize());
        }

        [Test]
        public void SynchronizeCorrectlyMovesEntries()
        {
            var result = baseSnapshot.Syncronize(syncSnapshot);
            var file6 = result.Entries.FirstOrDefault(e => e.Path == "file6");
            var file7 = result.Entries.FirstOrDefault(e => e.Path == "file7");

            Assert.IsNull(file6);
            Assert.NotNull(file7);

            Assert.AreEqual(EntryAction.Move, file7.Action);
            Assert.AreEqual("file6", file7.MoveFrom);
        }

        [Test]
        public void SynchronizeCorrectlyRemovesEntries()
        {
            var result = baseSnapshot.Syncronize(syncSnapshot);
            var file1 = result.Entries.FirstOrDefault(e => e.Path == "file1");

            Assert.NotNull(file1);
            Assert.AreEqual(EntryAction.Remove, file1.Action);
        }

        [Test]
        public void SynchronizeCorrectlyIgnoresEntries()
        {
            var result = baseSnapshot.Syncronize(syncSnapshot);
            var file2 = result.Entries.FirstOrDefault(e => e.Path == "file2");
            var file3 = result.Entries.FirstOrDefault(e => e.Path == "file3");

            Assert.NotNull(file2);
            Assert.AreEqual(EntryAction.None, file2.Action);

            Assert.NotNull(file3);
            Assert.AreEqual(EntryAction.None, file3.Action);
        }

        [Test]
        public void SynchronizeCorrectlyUpdatesEntries()
        {
            var result = baseSnapshot.Syncronize(syncSnapshot);
            var file4 = result.Entries.FirstOrDefault(e => e.Path == "file4");

            Assert.NotNull(file4);
            Assert.AreEqual(newDate, file4.LastModified);
            Assert.AreEqual(EntryAction.Add, file4.Action);
        }

        [Test]
        public void SynchronizeCorrectlyAddsEntries()
        {
            var result = baseSnapshot.Syncronize(syncSnapshot);
            var file5 = result.Entries.FirstOrDefault(e => e.Path == "file5");

            Assert.NotNull(file5);
            Assert.AreEqual(newDate, file5.LastModified);
            Assert.AreEqual(EntryAction.Add, file5.Action);
        }

        [Test]
        public void PatchDoesNotModifySources()
        {
            var originalBase = baseSnapshot.Serialize();
            var originalPatch = patchSnapshot.Serialize();
            _ = baseSnapshot.Patch(patchSnapshot);

            Assert.AreEqual(originalBase, baseSnapshot.Serialize());
            Assert.AreEqual(originalPatch, patchSnapshot.Serialize());
        }

        [Test]
        public void PatchCorrectlyRemovesEntries()
        {
            var result = baseSnapshot.Patch(patchSnapshot);
            var file1 = result.Entries.FirstOrDefault(e => e.Path == "file1");

            Assert.IsNull(file1);
        }

        [Test]
        public void PatchCorrectlyMovesEntries()
        {
            var patchSnapshot = new Snapshot()
            {
                Entries = new List<Snapshot.Entry>()
                {
                    new Snapshot.Entry() { LastModified=oldDate, Hash="F1", Path = "file6", Action = Snapshot.EntryAction.Move, MoveFrom = "file1" },
                    new Snapshot.Entry() { LastModified=newDate, Hash="F2", Path = "file7", Action = Snapshot.EntryAction.Move, MoveFrom = "file2" },
                }
            };
            var result = baseSnapshot.Patch(patchSnapshot);
            var file1 = result.Entries.FirstOrDefault(e => e.Path == "file1");
            var file2 = result.Entries.FirstOrDefault(e => e.Path == "file2");
            var file6 = result.Entries.FirstOrDefault(e => e.Path == "file6");
            var file7 = result.Entries.FirstOrDefault(e => e.Path == "file7");

            Assert.IsNull(file1);
            Assert.IsNull(file2);
            Assert.IsNotNull(file6);
            Assert.IsNotNull(file7);

            Assert.AreEqual(oldDate, file6.LastModified);
            Assert.AreEqual(newDate, file7.LastModified);
        }

        [Test]
        public void PatchCorrectlyUpdatesEntries()
        {
            var result = baseSnapshot.Patch(patchSnapshot);
            var file2 = result.Entries.FirstOrDefault(e => e.Path == "file2");
            var file5 = result.Entries.FirstOrDefault(e => e.Path == "file5");

            Assert.AreEqual(newDate, file2.LastModified);
            Assert.AreEqual(newDate, file5.LastModified);
        }

        [Test]
        public void PatchCorrectlyIgnoresEntries()
        {
            var result = baseSnapshot.Patch(patchSnapshot);
            var file3 = result.Entries.FirstOrDefault(e => e.Path == "file3");
            var file4 = result.Entries.FirstOrDefault(e => e.Path == "file4");

            Assert.AreEqual(oldDate, file3.LastModified);
            Assert.AreEqual(oldDate, file4.LastModified);
        }

        [Test]
        public void PatchResetsActionsIfCurrent()
        {
            // If we patch the same thing twice, the result should have no actions
            var result = baseSnapshot.Patch(patchSnapshot).Patch(patchSnapshot);
            var noActions = result.Entries.All(e => e.Action == Snapshot.EntryAction.None);

            Assert.IsTrue(noActions);
        }

        [Test]
        public void PatchResetsActions()
        {
            var result = baseSnapshot.Patch(patchSnapshot);
            var noActions = result.Entries.All(e => e.Action == Snapshot.EntryAction.None);

            Assert.IsTrue(noActions);
        }

        [Test]
        public void TrimDoesNotModifySource()
        {
            var originalBase = baseSnapshot.Serialize();
            var originalPatch = patchSnapshot.Serialize();
            var originalSync = syncSnapshot.Serialize();
            _ = baseSnapshot.Trim();
            _ = patchSnapshot.Trim();
            _ = syncSnapshot.Trim();

            Assert.AreEqual(originalBase, baseSnapshot.Serialize());
            Assert.AreEqual(originalPatch, patchSnapshot.Serialize());
            Assert.AreEqual(originalSync, syncSnapshot.Serialize());
        }

        [Test]
        public void TrimCorrectlyRemovesEntries()
        {
            var baseSnap = baseSnapshot.Trim();
            var patchSnap = baseSnapshot.Patch(patchSnapshot).Trim();
            var syncSnap = baseSnapshot.Syncronize(syncSnapshot).Trim();

            var baseIsEmpty = baseSnap.Entries.Count == 0;
            var patchIsEmpty = patchSnap.Entries.Count == 0;
            var syncHasAllFiles = HasAllFiles(syncSnap, "file1", "file4", "file5");
            var syncHasNoFiles = HasNoFiles(syncSnap, "file2", "file3");

            Assert.IsTrue(baseIsEmpty, "Base Is Empty");
            Assert.IsTrue(patchIsEmpty, "Patch Is Empty");
            Assert.IsTrue(syncHasAllFiles, "Sync Has All Files");
            Assert.IsTrue(syncHasNoFiles, "Sync Has No Files");
        }

        private bool HasAllFiles(Snapshot snap, params string[] files)
        {
            foreach (var file in files)
            {
                if (snap.Entries.Any(e=>e.Path == file) == false)
                {
                    return false;
                }
            }
            return true;
        }

        private bool HasNoFiles(Snapshot snap, params string[] files)
        {
            foreach (var file in files)
            {
                if (snap.Entries.Any(e => e.Path == file))
                {
                    return false;
                }
            }
            return true;
        }
    }
}