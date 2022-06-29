using System;

namespace Debug
{
    public static class DebugUtility
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static void DebugLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            string content = $"Caller[{caller.GetType().Name}]: {contents}";
            Console.WriteLine(content);
            logger.Debug(content);
            Reset();
        }

        public static void WarningLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string content = $"Caller[{caller.GetType().Name}]: {contents}";
            Console.WriteLine(content);
            logger.Warn(content);
            Reset();
        }

        public static void ErrorLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string content = $"Caller[{caller.GetType().Name}]: {contents}";
            Console.WriteLine(content);
            logger.Error(content);
            Reset();
        }

        private static void Reset()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}