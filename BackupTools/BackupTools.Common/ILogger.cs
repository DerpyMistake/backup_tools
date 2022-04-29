namespace BitEffects.BackupTools
{
    public enum LogEntryType { Info, Warning, Error }
    public interface ILogger
    {
        void AddEntry(LogEntryType type, string context, string message);
    }

    static public class ILoggerExtensions
    {
        static public void AddError(this ILogger logger, string context, string message)
        {
            logger.AddEntry(LogEntryType.Error, context, message);
        }

        static public void AddWarning(this ILogger logger, string context, string message)
        {
            logger.AddEntry(LogEntryType.Warning, context, message);
        }

        static public void AddInfo(this ILogger logger, string context, string message)
        {
            logger.AddEntry(LogEntryType.Info, context, message);
        }
    }
}
