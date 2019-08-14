using Xenko.Engine;

namespace VT49_Newer.Windows
{
    class VT49_NewerApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
