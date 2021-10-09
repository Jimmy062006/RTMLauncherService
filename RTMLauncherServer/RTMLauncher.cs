using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace RTMLauncherService
{
    public partial class RTMLauncher : ServiceBase
    {
        Process process;
        bool loaded = false;

        public RTMLauncher()
        {
            InitializeComponent();
            var timer = new System.Timers.Timer(10000);
            timer.Elapsed += new ElapsedEventHandler(Check_Running);
            timer.Enabled = true;
            eventLog1 = new EventLog();
            if (!EventLog.SourceExists("RTMLauncherService"))
            {
                EventLog.CreateEventSource(
                    "RTMLauncherService", "RTMLauncher");
            }
            eventLog1.Source = "RTMLauncherService";
            eventLog1.Log = "RTMLauncher";
        }

        private async void Check_Running(object sender, ElapsedEventArgs e)
        {
            if (loaded && process.HasExited)
            {
                await StartMiner().ConfigureAwait(false);
            }
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Waiting for debugger to attach");

#if DEBUG
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
#endif

            Console.WriteLine("Debugger attached");
            eventLog1.WriteEntry("Starting RTMLauncher.");
            StartMiner().ConfigureAwait(false);
        }

        private async Task<bool> StartMiner()
        {
            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = $"{Properties.Settings.Default.MinerPath}",
                        FileName = $"{Properties.Settings.Default.MinerPath}\\{Properties.Settings.Default.MinerExe}",
                        Arguments = $"-c {Properties.Settings.Default.MinerConfig}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.Exited += new EventHandler(ProcessQuit);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                loaded = true;
            }

            catch (Exception ex)
            {
                eventLog1.WriteEntry($"{ex}", EventLogEntryType.Error);
            }

            await Task.CompletedTask;
            return true;
        }

        private async void ProcessQuit(object sender, EventArgs e)
        {
            await StartMiner().ConfigureAwait(false);
        }

        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"{e.Data}");
                File.AppendAllText($"{Properties.Settings.Default.MinerPath}\\console.log", $"{e.Data}{Environment.NewLine}");
            }
            catch { }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Stopping RTMLauncher.");
            try
            {
                process.Kill();
            }
            catch { }
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("Resuming RTMLauncher.");
        }

        private void EventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }


    }
}
