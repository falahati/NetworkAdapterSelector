using System;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace NetworkAdapterSelector.Hook
{
    internal class CommandLineOptions
    {
        private static CommandLineOptions _defaultObject;


        private CommandLineOptions()
        {
        }

        public static CommandLineOptions Default
        {
            get
            {
                if (_defaultObject == null)
                {
                    _defaultObject = new CommandLineOptions();
                    Parser.Default.ParseArguments(Environment.GetCommandLineArgs().Skip(1).ToArray(), _defaultObject);
                    Console.WriteLine(string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
                    if (_defaultObject.LastParserState != null && _defaultObject.LastParserState.Errors.Count > 0)
                    {
                        throw new Exception(_defaultObject.LastParserState.Errors[0].ToString());
                    }
                }
                return _defaultObject;
            }
        }

        [Option('n', "network", Required = true, HelpText = "Identification string of the network adapter to bind.")]
        public string NetworkId { get; set; }

        [Option('e', "execute", Required = false, HelpText = "Address of the executable find to start.")]
        public string Execute { get; set; }

        [Option('c', "args", Required = false, HelpText = "Arguments to be send to the executable while starting.")]
        public string Arguments { get; set; }

        [Option('a', "attach", Required = false, HelpText = "PID of the process to attach.")]
        public int Attach { get; set; }

        [Option('t', "delay", Required = false, HelpText = "Delay in milliseconds before trying to inject the code.",
            DefaultValue = 1000)]
        public int Delay { get; set; }

        [Option('d', "debug", Required = false,
            HelpText =
                "Debug mode creates a log file in temp directory logging all the activities of the injected code.")]
        public bool Debug { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}