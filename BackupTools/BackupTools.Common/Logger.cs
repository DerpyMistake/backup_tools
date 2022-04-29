using System;

namespace BitEffects.BackupTools.DB
{
    public class Logger : ILogger
    {
        readonly IDatabase database;
        readonly string appName;

        public Logger(IDatabase database, string appName = null)
        {
            this.database = database;
            this.appName = appName ?? System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
        }

        public void AddEntry(LogEntryType type, string context, string message)
        {
            database.LogEntries.Add(new LogEntry()
            {
                Time = DateTime.UtcNow,
                AppName = appName,
                Type = type,
                Context = context,
                Message = message,
            });
        }
    }
}
