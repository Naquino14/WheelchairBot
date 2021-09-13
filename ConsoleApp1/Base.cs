using System;

namespace WheelchairBot
{
    public class Base
    {
        private static bool checkArgsFO = true;

        private static RunMode currentMode;
        public enum RunMode
        {
            dev,
            debug,
            normal,
            anncmnt
        }

        static void Main(string[] args)
        {
            if (checkArgsFO)
            {
                if (args[0] == "")
                {
                    Console.WriteLine("Critical error! No launch parameters inputted. The two available are: [-dev] [-normal]");
                    throw new InvalidOperationException();
                }
                else
                    StartupWithArgs(args[0]);
                checkArgsFO = false;
            }

            Bot bot = new Bot();
            bot.RunAsync(currentMode).GetAwaiter().GetResult();
        }

        private static void StartupWithArgs(string arg)
        {
            if (arg == "-dev")
                currentMode = RunMode.dev;
            else if (arg == "-debug")
                currentMode = RunMode.debug;
            else if (arg == "-normal")
                currentMode = RunMode.normal;
            else if (arg == "-anncmnt")
                currentMode = RunMode.anncmnt;
            else
            {
                Console.WriteLine("Error! Invalid launch parameter(s)");
                throw new InvalidOperationException();
            }

        }
    }
}
