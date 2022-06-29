using NLog;
using System;

namespace Debug
{
    public static class DebugUtility
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static void DebugLog(string contents)
        {
            logger.Debug(contents);
        }

        public static void WarningLog(string contents)
        {
            logger.Warn(contents);
        }

        public static void ErrorLog(string contents)
        {
            logger.Error(contents);
        }
    }
}