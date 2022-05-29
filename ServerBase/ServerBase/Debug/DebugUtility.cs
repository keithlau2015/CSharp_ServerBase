using System;

namespace Debug
{
    public static class DebugUtility
    {
        public static void DebugLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Caller[{caller.GetType().Name}]: {contents}");
            Reset();
        }

        public static void WarningLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Caller[{caller.GetType().Name}]: {contents}");
            Reset();
        }

        public static void ErrorLog(object caller, string contents)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Caller[{caller.GetType().Name}]: {contents}");
            Reset();
        }

        private static void Reset()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}