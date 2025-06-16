using System;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// Main entry point for the Game Server executable
    /// Supports command-line arguments for Unity integration
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Use the ServerLauncher with command-line support for Unity integration
            await ServerLauncher.Main(args);
        }
    }
} 