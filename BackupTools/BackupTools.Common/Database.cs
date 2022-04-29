using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace BitEffects.BackupTools.DB
{
    public class Database : IDatabase, IDisposable
    {
        const double UPDATE_INTERVAL_MS = 5000;

        const string DB_LOG_ENTRIES = "logEntries.json";
        const string DB_BACKUP_ENTRIES = "backupEntries.json";

        readonly static System.Threading.Mutex dbLocker = new System.Threading.Mutex(false, "BitEffects.BackupTools.DB");
        readonly private Dictionary<Type, int> initialCounts = new Dictionary<Type, int>();
        readonly private Options options;
        
        private Timer saveTimer = null;

        public List<LogEntry> LogEntries { get; private set; }
        public List<BackupEntry> BackupEntries { get; private set; }

        public Database(Options options)
        {
            this.options = options;
            
            this.Load();
            this.BeginSaveThread();
        }

        public void Load()
        {
            using var mtxLock = new MutexLock(dbLocker);

            this.LogEntries = LoadFile<LogEntry>(DB_LOG_ENTRIES);
            this.BackupEntries = LoadFile<BackupEntry>(DB_BACKUP_ENTRIES);
        }

        private List<T> LoadFile<T>(string fname, bool rememberCount = true)
        {
            List<T> res = null;

            string path = Path.Combine(options.DBPath, fname);
            if (File.Exists(path))
            {
                res = File.ReadAllText(path).Deserialize<List<T>>();
            }

            if (rememberCount)
            {
                initialCounts[typeof(T)] = res?.Count ?? 0;
            }

            return res ?? new List<T>();
        }

        public void Save()
        {
            using var mtxLock = new MutexLock(dbLocker);

            saveFile(this.LogEntries, DB_LOG_ENTRIES);
            saveFile(this.BackupEntries, DB_BACKUP_ENTRIES);

            void saveFile<T>(List<T> data, string fname)
            {
                // Inject any new records since the last time we read this data
                if (initialCounts.TryGetValue(typeof(T), out int count))
                {
                    var entries = LoadFile<T>(fname, false);
                    if (entries.Count > count)
                    {
                        data.InsertRange(count, entries.Skip(count));
                    }
                }

                string path = Path.Combine(options.DBPath, fname);
                string json = data.Serialize();

                Directory.CreateDirectory(options.DBPath);
                File.WriteAllText(path, json);

                initialCounts[typeof(T)] = data.Count;
            }
        }

        private void BeginSaveThread()
        {
            if (saveTimer != null)
            {
                saveTimer.Enabled = false;
                saveTimer.Dispose();
            }

            saveTimer = new Timer(UPDATE_INTERVAL_MS)
            {
                AutoReset = true,
                Enabled = true
            };
            saveTimer.Elapsed += (s, e) =>
            {
                saveTimer.Enabled = false;
                this.Save();
                saveTimer.Enabled = true;
            };
            saveTimer.Start();
        }

        public void Dispose()
        {
            this.Save();

            saveTimer?.Stop();
            saveTimer?.Dispose();
        }
    }
}
