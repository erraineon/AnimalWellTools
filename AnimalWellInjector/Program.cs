using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;
using EasyHook;

namespace AnimalWellInjector
{
    public class Program
    {
        public static void Main()
        {
            var channelName = default(string);
            var server = new HookInterface();
            RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, server);

            try
            {
                var hookLibrary = $"{typeof(HookInterface).Assembly.GetName().Name}.exe";
                const string processName = "Animal Well";
                var process = Process.GetProcessesByName(processName).FirstOrDefault() ??
                              throw new Exception($"No process named \"{processName}\" was found.");
                RemoteHooking.Inject(process.Id, hookLibrary, hookLibrary, channelName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while creating hook: {e}");
            }

            Console.WriteLine("Press the Enter key to exit.");
            Console.ReadLine();

            server.Stop();
            // Give it some time to dispose
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}