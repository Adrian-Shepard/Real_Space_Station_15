using System;

namespace SS15.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            using var game = new MainGame();
            game.Run();
        }
    }
}