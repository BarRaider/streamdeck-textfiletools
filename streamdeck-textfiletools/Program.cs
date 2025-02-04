using BarRaider.SdTools;
using BRUtils;

namespace BarRaider.TextFileUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
            
            SDWrapper.Run(args, new UpdateHandler());
        }
    }
}
