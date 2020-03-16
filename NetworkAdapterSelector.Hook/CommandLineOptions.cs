using System;
using System.Linq;
using CommandLine;

namespace NetworkAdapterSelector.Hook
{
    internal class CommandLineOptions
    {
        private static CommandLineOptions _defaultObject;

        [Option(
            'c',
            "args",
            Required = false,
            HelpText = "Arguments to be send to the executable while starting."
        )]
        public string Arguments { get; set; }

        [Option(
            'a',
            "attach",
            Required = false,
            HelpText = "PID of the process to attach."
        )]
        public int Attach { get; set; }

        [Option(
            'd',
            "debug",
            Required = false,
            HelpText =
                "Debug mode creates a log file in temp directory logging all the activities of the injected code."
        )]
        public bool Debug { get; set; }

        public static CommandLineOptions Default
        {
            get
            {
                if (_defaultObject == null)
                {
                    var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

                    for (var i = 0; i < args.Length; i++)
                    {
                        if ((args[i].ToLower().Trim() == "-c" || args[i].ToLower().Trim() == "--args") &&
                            i < args.Length - 1)
                        {
                            args[i + 1] = '"' + args[i + 1] + '"';
                        }
                    }

                    Parser.Default.ParseArguments<CommandLineOptions>(args)
                        .WithParsed(options =>
                        {
                            _defaultObject = options;
                        }).WithNotParsed(errors =>
                        {
                            Environment.Exit(1);
                        });
                }

                return _defaultObject;
            }
        }

        [Option(
            't',
            "delay",
            Required = false,
            HelpText = "Delay in milliseconds before trying to inject the code.",
            Default = 0
        )]
        public int Delay { get; set; }

        [Option(
            'e',
            "execute",
            Required = false,
            HelpText = "Address of the executable find to start."
        )]
        public string Execute { get; set; }

        [Option(
            'w',
            "title",
            Required = false,
            Default = true,
            HelpText =
                "Should the title of the process main's window be updated to contain the binded adapter information"
        )]

        public bool ChangeWindowTitle { get; set; } = true;

        [Option(
            'n',
            "network",
            Required = true,
            HelpText = "Identification string of the network adapter to bind."
        )]
        public string NetworkId { get; set; }
    }
}