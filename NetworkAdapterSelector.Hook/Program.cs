using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using EasyHook;
using ThreadState = System.Diagnostics.ThreadState;

namespace NetworkAdapterSelector.Hook
{
    internal class Program
    {
        private static void CreateAndInjectProcess(string networkId, string filePath, string arguments)
        {
            var injectorAddress = Assembly.GetExecutingAssembly().Location;
            var tries = 0;
            var processId = 0;

            while (true)
            {
                try
                {
                    Console.WriteLine("Trying to create the process as suspended.");
                    RemoteHooking.CreateAndInject(
                        filePath,
                        arguments,
                        0,
                        injectorAddress,
                        injectorAddress,
                        out processId,
                        networkId,
                        injectorAddress,
                        CommandLineOptions.Default.Delay,
                        CommandLineOptions.Default.ChangeWindowTitle,
                        CommandLineOptions.Default.Debug);

                    var process = Process.GetProcessById(processId);

                    Thread.Sleep(2000);

                    if (process.HasExited)
                    {
                        return;
                    }

                    if (IsProcessStuckByInjection(process))
                    {
                        Console.WriteLine("Process stuck in suspended state.");

                        // Create and Inject failed
                        if (!process.HasExited)
                        {
                            Console.WriteLine("Killing process ...");
                            process.Kill();
                            process.WaitForExit(3000);
                        }

                        throw new AccessViolationException("Failed to start the application.");
                    }

                    return;
                }
                catch
                {
                    if (tries < 3 && processId == 0)
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


        // ReSharper disable once TooManyDeclarations
        private static bool IsProcessStuckByInjection(Process process)
        {
            var allThreads = process.Threads.Cast<ProcessThread>().ToArray();

            var suspendedThreads = allThreads
                .Where(thread =>
                    thread.ThreadState == ThreadState.Wait &&
                    thread.WaitReason == ThreadWaitReason.Suspended
                )
                .ToArray();

            var lpcStuckThreads = allThreads
                .Where(thread =>
                    thread.ThreadState == ThreadState.Wait &&
                    thread.WaitReason != ThreadWaitReason.Suspended
                )
                .ToArray();

            var activeThreads = allThreads
                .Where(thread =>
                    thread.ThreadState != ThreadState.Wait
                )
                .ToArray();

            return activeThreads.Length == 0 &&
                   allThreads.Length - 1 >= suspendedThreads.Length &&
                   (lpcStuckThreads.Length == 0 ||
                    lpcStuckThreads.Length == 1 &&
                    lpcStuckThreads.FirstOrDefault()?.Id == allThreads.FirstOrDefault()?.Id);
        }

        private static void InjectProcess(string networkId, int processId)
        {
            var injectorAddress = Assembly.GetExecutingAssembly().Location;
            var tries = 0;

            while (true)
            {
                try
                {
                    Console.WriteLine("Trying to inject process #{0}.", processId);
                    RemoteHooking.Inject(
                        processId,
                        injectorAddress,
                        injectorAddress,
                        networkId,
                        injectorAddress,
                        CommandLineOptions.Default.Delay,
                        CommandLineOptions.Default.ChangeWindowTitle,
                        CommandLineOptions.Default.Debug
                    );

                    return;
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

                    Console.WriteLine("Creating process ...");

                    if (CommandLineOptions.Default.Delay <= 0)
                    {
                        try
                        {
                            CreateAndInjectProcess(
                                networkId,
                                CommandLineOptions.Default.Execute,
                                CommandLineOptions.Default.Arguments?.Trim('"') ?? ""
                            );

                            Console.WriteLine("SUCCESS");
                            Environment.Exit(0);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine("Failed to start process. Fallback to delayed injection.");
                        }
                    }

                    // Delayed attach
                    processId = Process.Start(
                        CommandLineOptions.Default.Execute,
                        CommandLineOptions.Default.Arguments?.Trim('"') ?? ""
                    )?.Id ?? 0;
                }

                if (processId <= 0 || Process.GetProcesses().All(p => p.Id != processId))
                {
                    throw new ArgumentException("Invalid process id provided or failed to start the process.");
                }

                Thread.Sleep(Math.Max(CommandLineOptions.Default.Delay, 1000));

                InjectProcess(networkId, processId);

                Console.WriteLine("SUCCESS");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                Console.WriteLine("FATAL");
                Environment.Exit(1);
            }
        }
    }
}