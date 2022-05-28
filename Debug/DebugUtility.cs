namespace Debug
{
    public static class DebugUtility
    {
        public static void DebugLog(string contents)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(contents);
            Reset();
        }

        public static void WarningLog(string contents)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(contents);
            Reset();
        }

        public static void ErrorLog(string contents)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(contents);
            Reset();
        }

        private static void Reset()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}