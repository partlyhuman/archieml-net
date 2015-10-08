using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArchieML {
    public class AmlCommandLine {

        enum Format {
            json,
            xml,
        }

        class Options {
            [Option('i', "input", MutuallyExclusiveSet = "in", HelpText = "An ArchieML file to read in.")]
            public string InputFile { get; set; }

            [Option('s', "stream", MutuallyExclusiveSet = "in", HelpText = "Read from standard input.")]
            public bool InputStream { get; set; }

            [Option('f', "format", DefaultValue = Format.json, HelpText = "Format to output (json or xml).")]
            public Format Format { get; set; }

            [HelpOption('h', "help")]
            public string Help() {
                var help = new HelpText() {
                    Heading = new HeadingInfo("aml - a commandline ArchieML parser and converter"),
                    Copyright = new CopyrightInfo("Roger Braunstein", 2015),
                    AddDashesToOption = true,
                };
                help.AddPreOptionsLine("For more information, see http://archieml.org");
                help.AddOptions(this);
                return help;
            }
        }

        static void Main(string[] args) {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options)) {
                TextReader input;
                if (options.InputStream) {
                    input = Console.In;
                }
                else if (options.InputFile != null) {
                    input = File.OpenText(options.InputFile);
                }
                else {
                    Console.WriteLine("Please specify either an input file using -i or standard in using -s.\n");
                    Console.WriteLine(options.Help());
                    return;
                }

                JObject root = Archie.Load(input);
                switch (options.Format) {
                    case Format.json:
                        Console.WriteLine(root.ToString());
                        break;
                    case Format.xml:
                        Console.WriteLine(JsonConvert.DeserializeXNode(root.ToString(), "root").ToString());
                        break;
                }
            }
        }
    }
}
