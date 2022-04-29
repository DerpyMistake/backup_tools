using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BitEffects.BackupTools.Sync
{
    public class Snapshot
    {
        public enum EntryAction { None, Add, Remove, Move }
        public class Entry
        {
            public Entry() { }

            public Entry(FileInfo fi, string basePath = "")
            {
                this.Action = EntryAction.None;
                this.LastModified = fi.LastWriteTimeUtc;
                this.Length = fi.Length;
                this.Path = fi.FullName.Substring(basePath.Length).TrimStart('/', '\\');

                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(fi.FullName))
                    {
                        this.Hash = Convert.ToBase64String(md5.ComputeHash(stream));
                    }
                }
            }

            public Entry(Entry copy)
            {
                this.Action = copy.Action;
                this.Hash = copy.Hash;
                this.LastModified = copy.LastModified;
                this.Length = copy.Length;
                this.MoveFrom = copy.MoveFrom;
                this.Path = copy.Path;
            }

            public EntryAction Action { get; set; }
            public string Hash { get; set; }
            public DateTime LastModified { get; set; }
            public long Length { get; set; }
            public string MoveFrom { get; set; }
            public string Path { get; set; }
        }

        public DateTime Date { get; set; } = DateTime.UtcNow;
        public List<Entry> Entries { get; set; } = new List<Entry>();

        /// <summary>
        /// Apply a patch and return the generated snapshot
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Snapshot Patch(Snapshot target)
        {
            var removed = target.Entries
                .Where(e => e.Action == EntryAction.Remove)
                .Select(e => e.Path)
                .ToHashSet();

            var res = new Snapshot()
            {
                Date = target.Date,
                Entries = this.Entries
                    .Where(e => !removed.Contains(e.Path))
                    .Select(e => new Entry(e) { Action = EntryAction.None })
                    .ToList()
            };

            var existing = res.Entries.ToDictionary(e => e.Path);
            foreach (var entry in target.Entries.Where(e => e.Action == EntryAction.Add))
            {
                if (existing.TryGetValue(entry.Path, out var ent))
                {
                    if (ent.LastModified < entry.LastModified)
                    {
                        ent.LastModified = entry.LastModified;
                    }
                }
                else
                {
                    res.Entries.Add(new Entry(entry) { Action = EntryAction.None });
                }
            }

            foreach (var entry in target.Entries.Where(e => e.Action == EntryAction.Move))
            {
                if (existing.TryGetValue(entry.MoveFrom, out var ent))
                {
                    ent.LastModified = entry.LastModified;
                    ent.Path = entry.Path;
                }
                else
                {
                    res.Entries.Add(new Entry(entry) { Action = EntryAction.None });
                }
            }

            return res;
        }

        /// <summary>
        /// Mark the differences between two snapshots and return the result
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Snapshot Syncronize(Snapshot target)
        {
            var res = new Snapshot()
            {
                Entries = this.Entries
                    .Select(e => new Entry(e))
                    .ToList()
            };

            var existingByHash = res.Entries
                .GroupBy(e => e.Hash)
                .Where(g => g.Skip(1).Take(1).Count() == 0)
                .ToDictionary(g => g.Key, g => g.First());

            var existingEntries = res.Entries.ToDictionary(e => e.Path);
            var mergingEntries = target.Entries.Select(e => e.Path).ToHashSet();

            // Mark all of the entries that need to be added/updated
            {
                foreach (var entry in target.Entries)
                {
                    if (existingEntries.TryGetValue(entry.Path, out var updatedEntry))
                    {
                        if (updatedEntry.LastModified < entry.LastModified)
                        {
                            updatedEntry.LastModified = entry.LastModified;
                            updatedEntry.Action = EntryAction.Add;
                        }
                    }
                    else if (existingByHash.TryGetValue(entry.Hash, out var existingEntry))
                    {
                        if (existingEntry.LastModified == entry.LastModified)
                        {
                            res.Entries.Remove(existingEntry);
                            res.Entries.Add(new Entry(entry)
                            {
                                Action = EntryAction.Move,
                                MoveFrom = existingEntry.Path
                            });
                        }
                    }
                    else
                    {
                        res.Entries.Add(new Entry(entry)
                        {
                            Action = EntryAction.Add
                        });
                    }
                }
            }

            // Mark all of the entries that need to be removed
            {
                foreach (var entry in this.Entries)
                {
                    if (mergingEntries.Contains(entry.Path) == false
                        && existingEntries.TryGetValue(entry.Path, out var ent))
                    {
                        ent.Action = EntryAction.Remove;
                    }
                }
            }

            return res;
        }

        public Snapshot Trim()
        {
            return new Snapshot()
            {
                Entries = this.Entries
                    .Where(e => e.Action != EntryAction.None)
                    .Select(e => new Entry(e))
                    .ToList()
            };
        }
    }
}
