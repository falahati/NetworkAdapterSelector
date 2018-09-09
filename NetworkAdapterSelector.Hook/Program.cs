using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using EasyHook;

namespace NetworkAdapterSelector.Hook
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                var processId = CommandLineOptions.Default.Attach;
                var networkId = CommandLineOptions.Default.NetworkId.Trim().ToLower();
                var injectorAddress = Assembly.GetExecutingAssembly().Location;
                if (!NetworkInterface.GetAllNetworkInterfaces()
                    .Any(
                        @interface =>
                            @interface.Id.Equals(networkId, StringComparison.CurrentCultureIgnoreCase) &&
                            @interface.SupportsMulticast &&
                            @interface.OperationalStatus == OperationalStatus.Up))
                {
                    throw new Exception("Selected network id is invalid or the selected interface is not functional.");
                }
                if (processId <= 0 && string.IsNullOrWhiteSpace(CommandLineOptions.Default.Execute))
                {
                    throw new Exception("Nothing to do.");
                }
                if (!string.IsNullOrWhiteSpace(CommandLineOptions.Default.Execute))
                {
                    if (!File.Exists(CommandLineOptions.Default.Execute))
                    {
                        throw new IOException("File not found.");
                    }
                    processId =
                        Process.Start(CommandLineOptions.Default.Execute, CommandLineOptions.Default.Arguments)?.Id ?? 0;
                }
                if (processId > 0 && Process.GetProcesses().All(p => p.Id != processId))
                {
                    throw new Exception("Invalid process id provided or failed to start the process.");
                }
                Thread.Sleep(CommandLineOptions.Default.Delay);
                RemoteHooking.Inject(processId, injectorAddress, injectorAddress, networkId, injectorAddress,
                    CommandLineOptions.Default.Delay, CommandLineOptions.Default.Debug);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Environment.Exit(0);
        }
    }
}
