using System;
using System.Xml;
using System.Timers;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private static List<List<string>> OpcItemsNamed = new List<List<string>>();
        private static serialPort Serialport;

        private static OpcClient[] OpcClient = new OpcClient[_MaxmumOpcClientInstance];
        private static XmlOperator[] XmlHandler = new XmlOperator[_MaxmumOpcClientInstance];
        private static string[] XmlPath = new string[_MaxmumOpcClientInstance];
        private static string[] ServerName = new string[_MaxmumOpcClientInstance];
        private static string[] ServerHost = new string[_MaxmumOpcClientInstance];
        private static string[] ConfigFileName = new string[_MaxmumOpcClientInstance];
        private static string SendStr;

        private static bool HasOpcRiver;
        private static string OpcRiverHost, OpcRiverName, OpcRiverItemsFile;
        private static List<string> OpcRiverItems = new List<string>();
        private static OpcClient OpcRiver;
        private static List<string> OpcRiverTags = new List<string>();

        private static DateTime localDate;

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
                    if (!OpcClient[ServerInd].OpcIsConnected())
                    {
                        Console.WriteLine($"OpcServer_{ServerInd} connection is failed, reconnecting...");
                        Reconnect2OpcServer(ServerInd);
                        continue;
                    }
                    OpcDaItemValue[] items = OpcClient[ServerInd].ReadOpcDaValues();
                    foreach (var item in items)
                    {
                        XmlHandler[ServerInd].ModifyNodeValue(item.Item.ItemId, item.Value.ToString());
                    }
                    XmlHandler[ServerInd].SavingFile();

                    if (Serialport.IsOpened())
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
                        Serialport = new serialPort(ComName, 115200, Parity.None, 8, StopBits.One, ReceivedFilePath);
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                }
            }
            if (HasOpcRiver)
            {
                if (Serialport.hasNewData)
                {
                    List<string> values = new List<string>();
                    foreach(string item in OpcRiverItems)
                    {
                        string path = $"Data//PowerPlant//{ReDefinitionPath(item)}";
                        XmlNode node = Serialport.xml.SelectSingleNode(path);
                        values.Add(node.InnerText);
                    }
                    OpcRiver.WriteOpcRiverDaValues(OpcRiverTags.ToArray(), values.ToArray());
                }
            }
        }

        Regex regNum = new Regex("^[0-9]");
        private string ReDefinitionPath(string root)
        {
            if (regNum.IsMatch(root)) root = $"i{root}";
            return root.Replace(@"$", @"s3b1").Replace(@":", @"s3b2").Replace(@"/", @"s3b3").Replace(@"@", @"s3b4").Replace(@"_", @"s3b5").Replace(@"-", @"s3b6").Replace(@"+", @"s3b7").Replace(@"#", "s3b8");
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
            OpcClient[ServerInd].AddOpcDaItems(OpcItemsNamed[ServerInd].ToArray());
            localDate = DateTime.Now;
            if (OpcClient[ServerInd].OpcIsConnected()) {
                string[] Msg = new string[] { $"{localDate}\t\t\n Server_{ServerInd} is Connected." };
                File.AppendAllLines(@"./OpcConnectionMessage.log", Msg);
            }
            else
            {
                string[] Msg = new string[] { $"{localDate}\t\t\n Server_{ServerInd} Connection failed." };
                File.AppendAllLines(@"./OpcConnectionMessage.log", Msg);
            }
        }

        static void Init()
        {
            //Todo: INI File Handler
            SetupIni iniFile = new SetupIni(iniFileName, 1024);
            ComName = iniFile.IniReadValue(@"Customize", @"ComPort");
            ReceivedFilePath = iniFile.IniReadValue(@"Customize", @"ReceivedFilePath");
            updRate = Convert.ToInt32(iniFile.IniReadValue(@"Customize", @"UpdateRate"));
            Serialport = new serialPort(ComName, 115200, Parity.None, 8, StopBits.One, ReceivedFilePath);
            HasOpcRiver = Convert.ToBoolean(iniFile.IniReadValue(@"Customize", @"HasOpcRiver"));
            //OpcRiver Statement
            if (HasOpcRiver)
            {
                OpcRiverName = iniFile.IniReadValue(@"OpcRiver", @"OpcRiverName");
                OpcRiverHost = iniFile.IniReadValue(@"OpcRiver", @"OpcRiverHost");
                OpcRiverItemsFile = iniFile.IniReadValue(@"OpcRiver", @"OpcRiverItemsFile");
                OpcRiver = new OpcClient(OpcRiverName, OpcRiverHost);
                try
                {
                    for(int i = 1; i <= 8; i++)
                    {
                        OpcRiverTags.Add($"Channel1.Device2.M{i}KW");
                    }
                    if (File.Exists(OpcRiverItemsFile))
                    {
                        using (StreamReader sr = new StreamReader(OpcRiverItemsFile))
                        {
                            while (sr.Peek() >= 0)
                            {
                                string ret = sr.ReadLine();
                                OpcRiverItems.Add(ret);
                            }
                            if (OpcRiver.OpcIsConnected())
                                OpcRiver.AddOpcDaItems(OpcRiverTags.ToArray());
                        }
                        Console.WriteLine($"Reading OpcRiver Configuration and Initializing..." +
                            $"\nServerName => {OpcRiverName}" +
                            $"\nServerHost => {OpcRiverHost}");
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            //OpcRiver Statement
            ServerCnt = 0;
            while (!iniFile.IniReadValue(@"SystemCfg", $"OpcServerName_{ServerCnt}").Equals(@"NotFound"))
            {
                Console.WriteLine($"\nInit Number: {ServerCnt} Process has started.");
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
                            List<string> ret = new List<string>();
                            while (sr.Peek() >= 0)
                            {
                                string tmp = sr.ReadLine();
                                ret.Add(tmp);
                                XmlHandler[ServerCnt].AddNode(tmp);
                            }
                            OpcItemsNamed.Add(ret);
                            if (OpcClient[ServerCnt].OpcIsConnected())
                                OpcClient[ServerCnt].AddOpcDaItems(ret.ToArray());
                        }

                        Console.WriteLine($"Reading Service_{ServerCnt} Configuration and Initializing..." +
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
