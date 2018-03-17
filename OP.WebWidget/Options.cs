using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Windows.Forms;

namespace OP.WebWidget
{
    public class Options
    {

        public string AppName { get; private set; }

        [Option(longName: "url", HelpText = "The initial URL that will be loaded", Required = false)]
        public string URL { get; set; }

        [Option(longName: "save-session", HelpText = "If true then the browser session is stored and can be reused. If false then the browser is basically working in Anonymous mode.", Required = false)]
        public bool SaveSession { get; set; } = false;

        [Option(longName: "session-folder", HelpText = "The folder where the browser session information is stored if -s option is set. If not set then %LOCALAPPDATA%\\OP.WebWidged folder is used", Required = false)]
        public string SessionFolder { get; set; }

        [Option(longName: "screen", HelpText = "Screen on which the window should appear. If not set then primary screen will be used.", Required = false)]
        public int? Screen { get; set; }

        [Option(longName: "position", HelpText = "Initial position 'x,y' of the window. Negative values will offset the window from the right/bottom.", Required = false)]
        public Point? Position { get; set; }

        [Option(longName: "size", HelpText = "Initial size 'width,height' of the window", Required = false)]
        public Size? Size { get; set; }

        [Option(longName: "no-sendtoback", HelpText = "The window is not automatically send to back and kept under other windows when not active.", Required = false)]
        public bool NoSendToBack { get; set; }


        [Option(longName: "border-style", HelpText = "Window border style. Valid values: None, Fixed, Sizable.", Required = false)]
        public string BorderStyle { get; set; } = "Fixed";

        [Option(longName: "no-toolbar", HelpText = "Hide toolbar.", Required = false)]
        public bool NoToolbar { get; set; }

        [Option(longName: "no-toolbar-close", HelpText = "Hide toolbar close button.", Required = false)]
        public bool NoToolbarClose { get; set; }

        [Option(longName: "no-toolbar-prev", HelpText = "Hide toolbar previous page button.", Required = false)]
        public bool NoToolbarPrev { get; set; }

        [Option(longName: "no-toolbar-next", HelpText = "Hide toolbar next page button.", Required = false)]
        public bool NoToolbarNext { get; set; }

        [Option(longName: "no-toolbar-refresh", HelpText = "Hide toolbar refresh button.", Required = false)]
        public bool NoToolbarRefresh { get; set; }

        [Option(longName: "no-toolbar-home", HelpText = "Hide toolbar home button.", Required = false)]
        public bool NoToolbarHome { get; set; }

        [Option(longName: "config-file", HelpText = "File containing configuration file. Each line in the file must hold the command line options and each line will open a new window. Any additional options (except --url will be forwarded to new windows).", Required = false)]
        public string ConfigFile { get; set; }


        public Options()
        {
            AppName = typeof(Options).Assembly.GetName().Name;
            AssemblyTitleAttribute appTitleAtt = typeof(Options).Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true).OfType<AssemblyTitleAttribute>().FirstOrDefault();
            if (appTitleAtt != null)
                AppName = appTitleAtt.Title;
            SessionFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        }

        public bool Load(String[] args)
        {
            var parser = new CommandLine.Parser((settings) =>
            {
                settings.CaseSensitive = false;
                settings.HelpWriter = null;
            });
            return parser.ParseArguments(args, this);
        }


        public string GetUsage()
        {
            String usage = CommandLine.Text.HelpText.AutoBuild(this, (current) => CommandLine.Text.HelpText.DefaultParsingErrorsHandler(this, current));
            return usage;
        }

        public string GetCurrentParameters()
        {
            System.Text.StringBuilder sb = new StringBuilder();
            foreach (var prop in this.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
            {
                sb.AppendFormat("{0,-20} {1}", prop.Name, prop.GetValue(this));
                sb.AppendLine();
            }
            return sb.ToString();
        }

    }
}
