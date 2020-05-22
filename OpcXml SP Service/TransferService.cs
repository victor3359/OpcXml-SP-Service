using System;
using System.Timers;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using TitaniumAS.Opc.Client.Da;
using OpcXml_SP_Service.Libs;

namespace OpcXml_SP_Service
{
    public class TransferService
    {
        private readonly Timer _timer;
        private static readonly int _MaxmumOpcClientInstance = 10;

        private static string iniFileName = @"./OPConfig.ini";
        private static string ComName, ReceivedFilePath; //Common Parameters
        private static int updRate;
        private static int ServerCnt;

        private static List<string> OpcItemsNamed = new List<string>();
        private static serialPort Serialport;

        private static OpcClient[] OpcClient = new OpcClient[_MaxmumOpcClientInstance];
        private static XmlOperator[] XmlHandler = new XmlOperator[_MaxmumOpcClientInstance];
        private static string[] XmlPath = new string[_MaxmumOpcClientInstance];
        private static string[] ServerName = new string[_MaxmumOpcClientInstance];
        private static string[] ServerHost = new string[_MaxmumOpcClientInstance];
        private static string[] ConfigFileName = new string[_MaxmumOpcClientInstance];
        private static string SendStr;

        public TransferService()
        {
            Init();
            _timer = new Timer(updRate) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e) 
        {
            SendStr = "";
            for (int ServerInd = 0; ServerInd < ServerCnt; ServerInd++)
            {
                try
                {
                    OpcDaItemValue[] items = OpcClient[ServerInd].ReadOpcDaValues();
                    foreach (var item in items)
                    {
                        XmlHandler[ServerInd].ModifyNodeValue(item.Item.ItemId, item.Value.ToString());
                    }
                    XmlHandler[ServerInd].SavingFile();

                    if (Serialport.isConnected)
                    {
                        if (ServerInd.Equals(ServerCnt - 1))
                        {
                            Serialport.sendDatas(SendStr + XmlHandler[ServerInd].doc.OuterXml);
                        }
                        else
                        {
                            SendStr += XmlHandler[ServerInd].doc.OuterXml;
                        }
                    }
                    else
                    {
                        Serialport = new serialPort(ComName, 115200, Parity.None, 8, StopBits.One, XmlPath[ServerInd]);
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    Reconnect2OpcServer(ServerInd);
                }
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        static void Reconnect2OpcServer(int ServerInd)
        {
            OpcClient[ServerInd] = new OpcClient(ServerName[ServerInd], ServerHost[ServerInd]);
            OpcClient[ServerInd].AddOpcDaItems(OpcItemsNamed.ToArray());
        }

        static void Init()
        {
            //Todo: INI File Handler
            SetupIni iniFile = new SetupIni(iniFileName, 1024);
            ComName = iniFile.IniReadValue(@"Customize", @"ComPort");
            Serialport = new serialPort(ComName, 115200, Parity.None, 8, StopBits.One, ReceivedFilePath);
            ReceivedFilePath = iniFile.IniReadValue(@"Customize", @"ReceivedFilePath");
            updRate = Convert.ToInt32(iniFile.IniReadValue(@"Customize", @"UpdateRate"));
            ServerCnt = 0;
            while (!iniFile.IniReadValue(@"SystemCfg", $"OpcServerName_{ServerCnt}").Equals(@"NotFound"))
            {
                ServerName[ServerCnt] = iniFile.IniReadValue(@"SystemCfg", $"OpcServerName_{ServerCnt}");
                ServerHost[ServerCnt] = iniFile.IniReadValue(@"SystemCfg", $"OpcServerHost_{ServerCnt}");
                XmlPath[ServerCnt] = iniFile.IniReadValue(@"SystemCfg", $"XmlPath_{ServerCnt}");
                ConfigFileName[ServerCnt] = iniFile.IniReadValue(@"SystemCfg", $"OpcItemsMappingFile_{ServerCnt}");

                OpcClient[ServerCnt] = new OpcClient(ServerName[ServerCnt], ServerHost[ServerCnt]);
                //Todo: XML Handler
                XmlHandler[ServerCnt] = new XmlOperator(ServerName[ServerCnt], ServerHost[ServerCnt], XmlPath[ServerCnt]);

                try
                {
                    if (File.Exists(ConfigFileName[ServerCnt]))
                    {
                        using (StreamReader sr = new StreamReader(ConfigFileName[ServerCnt]))
                        {
                            while (sr.Peek() >= 0)
                            {
                                string ret = sr.ReadLine();
                                OpcItemsNamed.Add(ret);
                                XmlHandler[ServerCnt].AddNode(ret);
                            }
                            OpcClient[ServerCnt].AddOpcDaItems(OpcItemsNamed.ToArray());
                        }

                        Console.WriteLine($"ReadService_{ServerCnt} Initializing..." +
                            $"\nServerName => {ServerName[ServerCnt]}" +
                            $"\nServerHost => {ServerHost[ServerCnt]}");
                    }
                    else
                    {
                        Console.WriteLine(@"File NOT exists.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"The process failed: {e.ToString()}");
                }
                ServerCnt++;
            }

        }
    }
}
