using NLog;
using NLog.Config;
using NLog.Targets;
using System;

namespace Debug
{
    public static class DebugUtility
    {
        private static int debugLevel = -1;
        private static bool isInit = false;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static void DebugLog(string contents)
        {
            if (debugLevel < 0)
                return;

            if (!isInit)
                Init();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{TimeManager.singleton.GetServerDatetime().ToString("MM/dd/yyyy HH:mm")}]: {contents}");

            logger.Debug(contents);
        }

        public static void WarningLog(string contents)
        {
            if (debugLevel < 0)
                return;

            if (!isInit)
                Init();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{TimeManager.singleton.GetServerDatetime().ToString("MM/dd/yyyy HH:mm")}]: {contents}");

            logger.Warn(contents);
        }

        public static void ErrorLog(string contents)
        {
            if (debugLevel < 0)
                return;

            if (!isInit)
                Init();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{TimeManager.singleton.GetServerDatetime().ToString("MM/dd/yyyy HH:mm")}]: {contents}");

            logger.Error(contents);
        }

        public static void Init(int targetDebugLevel = -1)
        {
            LoggingConfiguration config = new LoggingConfiguration();
            FileTarget fileTarget = new FileTarget
            {
                FileName = "${basedir}/logs/${shortdate}.log",
                Layout = "${date:format=yyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}"
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
            LogManager.Configuration = config;

            debugLevel = targetDebugLevel;

            isInit = true;
        }
    }
}