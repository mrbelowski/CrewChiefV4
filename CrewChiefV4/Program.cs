using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChiefV4
{
    static class Program
    {
        private static Dictionary<String, IntPtr> processorAffinities = new Dictionary<String, IntPtr> {
            { "cpu1", new IntPtr(0x0001) },
            { "cpu2", new IntPtr(0x0002) },
            { "cpu3", new IntPtr(0x0004) },
            { "cpu4", new IntPtr(0x0008) },
            { "cpu5", new IntPtr(0x0010) },
            { "cpu6", new IntPtr(0x0020) },
            { "cpu7", new IntPtr(0x0040) },
            { "cpu8", new IntPtr(0x0080) }
        };
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            String[] commandLineArgs = Environment.GetCommandLineArgs();
            Boolean allowMultipleInst = false;
            if (commandLineArgs != null)
            {
                foreach (String commandLineArg in commandLineArgs)
                {
                    if (processorAffinities.ContainsKey(commandLineArg))
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetCurrentProcess();
                            // Set Core 
                            process.ProcessorAffinity = processorAffinities[commandLineArg];
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Failed to set process affinity");
                        }
                    }
                    if(commandLineArg.Equals("multi"))
                    {
                        allowMultipleInst = true;
                    }
                }
                if(!allowMultipleInst)
                {
                    if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
                    {
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }                        
                }
            }            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
