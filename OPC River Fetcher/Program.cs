using System;
using System.IO;
using Topshelf;

namespace OPC_River_Fetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                x.Service<TransferService>(s =>
                {
                    s.ConstructUsing(TransferService => new TransferService());
                    s.WhenStarted(TransferService => TransferService.Start());
                    s.WhenStopped(TransferService => TransferService.Stop());
                });

                x.RunAsLocalSystem();

                x.SetServiceName(@"OpcXmlTransferService");
                x.SetDisplayName(@"OpcXml SP Transfer Service");
                x.SetDescription(@"OpcXml SP Transfer is a Service which can convert Opc information to Xml and send it to SerialPort. The Project is for Tashan by ICPSI.");


                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(TimeSpan.FromSeconds(5));
                    r.RestartService(TimeSpan.FromSeconds(5));
                    r.RestartComputer(TimeSpan.FromSeconds(30), @"Computer is restarting...");
                    r.SetResetPeriod(7);
                    r.OnCrashOnly();
                });
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}
