using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OP.WebWidget
{
    static class Program
    {
        private static bool _consoleAttached;
        private static bool _formCreated;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.ThreadException += Application_ThreadException;
            _consoleAttached = AttachConsole(-1);
            Options[] options = LoadOptions(args);
            if (options == null || options.Length == 0)
            {
                return -1;
            }

            var settings = new CefSettings();
            if (options[0].SaveSession)
            {
                if (!Directory.Exists(options[0].SessionFolder))
                    Directory.CreateDirectory(options[0].SessionFolder);

                settings.CachePath = options[0].SessionFolder;
                settings.PersistUserPreferences = true;
            }
            else
            {
                settings.CachePath = null;
                settings.PersistUserPreferences = false;

            }
            Cef.Initialize(settings);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _formCreated = true;
            List<Form> forms = new List<Form>();
            for(var i = 0; i < options.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(options[i].URL))
                {
                    var form = new FormBrowser(options[i]);
                    forms.Add(form);
                }
            }
            if (forms.Count == 0)
                return 0;
            WidgetAppContext ctx = new WidgetAppContext(forms.ToArray());
            Application.Run(ctx);
//            Application.Run(forms[0]);
            return 0;
        }

        private static Options[] LoadOptions(string[] args)
        {
            List<Options> result = new List<Options>();
            Options opt = new WebWidget.Options();
            if (!opt.Load(args))
            {
                if (!_consoleAttached)
                {
                    AllocConsole();
                    Console.Write(opt.GetUsage());
                    Console.WriteLine("Press any key to close ...");
                    Console.ReadKey();
                }
                else
                {
                    Console.Write(opt.GetUsage());
                }
                return null;
            }
            result.Add(opt);
            if (!string.IsNullOrWhiteSpace(opt.ConfigFile))
            {
                if (!File.Exists(opt.ConfigFile))
                    throw new FileNotFoundException(opt.ConfigFile);
                String[] lines = File.ReadAllLines(opt.ConfigFile);
                foreach(var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.Trim().StartsWith("#"))
                        continue;
                    var addOpt = LoadOptions(ConvertToArgs(line));
                    if (addOpt == null)
                        return null;
                    result.AddRange(addOpt);
                }
            }
            return result.ToArray();
        }



        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            if (_formCreated) {
                MessageBox.Show("An error occured: \n" + e.Exception.ToString(), nameof(OP.WebWidget), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (!_consoleAttached)
                {
                    AllocConsole();
                    Console.Write("An error occured: \n" + e.Exception.ToString());
                    Console.WriteLine("Press any key to close ...");
                    Console.ReadKey();
                }
                else
                {
                    Console.Write("An error occured: \n" + e.Exception.ToString());
                }
            }
        }

        /// <summary>
        /// Reads command line arguments from a single string.
        /// </summary>
        /// <param name="argsString">The string that contains the entire command line.</param>
        /// <returns>An array of the parsed arguments.</returns>
        public static string[] ConvertToArgs(string argsString)
        {
            // Collects the split argument strings
            List<string> args = new List<string>();
            // Builds the current argument
            var currentArg = new StringBuilder();
            // Indicates whether the last character was a backslash escape character
            bool escape = false;
            // Indicates whether we're in a quoted range
            bool inQuote = false;
            // Indicates whether there were quotes in the current arguments
            bool hadQuote = false;
            // Remembers the previous character
            char prevCh = '\0';
            // Iterate all characters from the input string
            for (int i = 0; i < argsString.Length; i++)
            {
                char ch = argsString[i];
                if (ch == '\\' && !escape)
                {
                    // Beginning of a backslash-escape sequence
                    escape = true;
                }
                else if (ch == '\\' && escape)
                {
                    // Double backslash, keep one
                    currentArg.Append(ch);
                    escape = false;
                }
                else if (ch == '"' && !escape)
                {
                    // Toggle quoted range
                    inQuote = !inQuote;
                    hadQuote = true;
                    if (inQuote && prevCh == '"')
                    {
                        // Doubled quote within a quoted range is like escaping
                        currentArg.Append(ch);
                    }
                }
                else if (ch == '"' && escape)
                {
                    // Backslash-escaped quote, keep it
                    currentArg.Append(ch);
                    escape = false;
                }
                else if (char.IsWhiteSpace(ch) && !inQuote)
                {
                    if (escape)
                    {
                        // Add pending escape char
                        currentArg.Append('\\');
                        escape = false;
                    }
                    // Accept empty arguments only if they are quoted
                    if (currentArg.Length > 0 || hadQuote)
                    {
                        args.Add(currentArg.ToString());
                    }
                    // Reset for next argument
                    currentArg.Clear();
                    hadQuote = false;
                }
                else
                {
                    if (escape)
                    {
                        // Add pending escape char
                        currentArg.Append('\\');
                        escape = false;
                    }
                    // Copy character from input, no special meaning
                    currentArg.Append(ch);
                }
                prevCh = ch;
            }
            // Save last argument
            if (currentArg.Length > 0 || hadQuote)
            {
                args.Add(currentArg.ToString());
            }
            return args.ToArray();
        }

        private class WidgetAppContext : ApplicationContext
        {

            private Form[] _forms;

            public WidgetAppContext(Form[] forms)
            {
                _forms = forms;
                foreach(Form form in forms) {
                    form.FormClosed += Form_FormClosed;
                }
                foreach (Form form in forms)
                {
                    form.Show();
                }
            }

            private void Form_FormClosed(object sender, FormClosedEventArgs e)
            {
                if (Application.OpenForms.Count == 0)
                    ExitThread();
            }
        }

    }
}
