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
                // Check for network id validity
                var networkId = CommandLineOptions.Default.NetworkId.Trim().ToLower();

                if (!NetworkInterface.GetAllNetworkInterfaces()
                    .Any(
                        @interface =>
                            @interface.Id.Equals(networkId, StringComparison.CurrentCultureIgnoreCase) &&
                            @interface.SupportsMulticast &&
                            @interface.OperationalStatus == OperationalStatus.Up))
                {
                    throw new ArgumentException(
                        "Selected network id is invalid or the selected interface is not functional.");
                }

                // Inject or create and inject to the program
                var processId = CommandLineOptions.Default.Attach;

                if (processId <= 0 && string.IsNullOrWhiteSpace(CommandLineOptions.Default.Execute))
                {
                    throw new InvalidOperationException("Nothing to do.");
                }

                if (!string.IsNullOrWhiteSpace(CommandLineOptions.Default.Execute))
                {
                    if (!File.Exists(CommandLineOptions.Default.Execute))
                    {
                        throw new FileNotFoundException("File not found.");
                    }

                    processId =
                        Process.Start(CommandLineOptions.Default.Execute,
                            CommandLineOptions.Default.Arguments?.Trim('"') ?? "")?.Id ??
                        0;
                }

                if (processId > 0 && Process.GetProcesses().All(p => p.Id != processId))
                {
                    throw new ArgumentException("Invalid process id provided or failed to start the process.");
                }

                Thread.Sleep(CommandLineOptions.Default.Delay);

                var injectorAddress = Assembly.GetExecutingAssembly().Location;
                var tries = 0;

                while (true)
                {
                    try
                    {
                        RemoteHooking.Inject(processId, injectorAddress, injectorAddress, networkId, injectorAddress,
                            CommandLineOptions.Default.Delay, CommandLineOptions.Default.Debug);

                        // Success
                        Environment.Exit(0);
                    }
                    catch
                    {
                        if (tries < 3)
                        {
                            tries++;
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                // Failure
                Environment.Exit(1);
            }
        }
    }
}