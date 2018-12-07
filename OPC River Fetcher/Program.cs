using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Xml;
using System.Runtime;
using System.Runtime.InteropServices;
using OPCAutomation;

namespace OPC_River_Fetcher
{
    class SetupIni
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section,
            string key, string def, StringBuilder retVal,
            int size, string filePath);

        private static string INIFilePath;  //INI檔案名稱

        public SetupIni(string iniFilePath)
        {
            INIFilePath = iniFilePath;
        }

        public void IniWriteValue(string Section, string Key, string Value) //INI寫入函式
        {
            WritePrivateProfileString(Section, Key, Value, System.Environment.CurrentDirectory + "\\" + INIFilePath);
        }

        public string IniReadValue(string Section, string Key)  //INI讀取函式
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, System.Environment.CurrentDirectory + "\\" + INIFilePath);
            return temp.ToString();
        }
    }
    class OPCHelper : IDisposable
    {
        private string strHostIP;
        private string strHostName;
        private OPCServer opcServer;
        private OPCGroups opcGroups;
        private OPCGroup opcGroup;
        private List<int> itemHandleClient = new List<int>();
        private List<int> itemHandleServer = new List<int>();
        private List<string> itemNames = new List<string>();
        private OPCItems opcItems;
        private OPCItem opcItem;
        private Dictionary<string, string> itemValues = new Dictionary<string, string>();
        public bool Connected = false;

        public OPCHelper(string strHostIP, string strHostName, int UpdateRate)
        {
            this.strHostIP = strHostIP;
            this.strHostName = strHostName;
            if (!CreateServer())
                return;
            if (!ConnectServer(strHostIP, strHostName))
                return;
            Connected = true;
            opcGroups = opcServer.OPCGroups;
            opcGroup = opcGroups.Add("OPCGroup_ICPSI");
            SetGroupProperty(opcGroup, UpdateRate);
            opcGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
            opcGroup.AsyncWriteComplete += new DIOPCGroupEvent_AsyncWriteCompleteEventHandler(opcGroup_AsyncWriteComplete);
            opcItems = opcGroup.OPCItems;
        }
        //創建Server
        private bool CreateServer()
        {
            try
            {
                opcServer = new OPCServer();
            }
            catch
            {
                return false;
            }
            return true;
        }
        //連線到Server
        private bool ConnectServer(string strHostIP, string strHostName)
        {
            try
            {
                opcServer.Connect(strHostName, strHostIP);
            }
            catch
            {
                return false;
            }
            return true;
        }
        //設定群組屬性
        private void SetGroupProperty(OPCGroup opcGroup, int updateRate)
        {
            opcGroup.IsActive = true;
            opcGroup.DeadBand = 0;
            opcGroup.UpdateRate = updateRate;
            opcGroup.IsSubscribed = true;
        }

        public bool Contains(string itemNameContains)
        {
            foreach (string key in itemValues.Keys)
            {
                if (key == itemNameContains)
                    return true;
            }
            return false;
        }

        public void AddItems(string[] itemNamesAdded)
        {
            for (int i = 0; i < itemNamesAdded.Length; i++)
            {
                this.itemNames.Add(itemNamesAdded[i]);
                itemValues.Add(itemNamesAdded[i], "");
            }
            for (int i = 0; i < itemNamesAdded.Length; i++)
            {
                itemHandleClient.Add(itemHandleClient.Count != 0 ? itemHandleClient[itemHandleClient.Count - 1] + 1 : 1);
                opcItem = opcItems.AddItem(itemNamesAdded[i], itemHandleClient[itemHandleClient.Count - 1]);
                itemHandleServer.Add(opcItem.ServerHandle);
            }
        }
        
        public string[] GetItemValues(string[] getValuesItemNames)
        {
            string[] getedValues = new string[getValuesItemNames.Length];
            for (int i = 0; i < getValuesItemNames.Length; i++)
            {
                if (Contains(getValuesItemNames[i]))
                    itemValues.TryGetValue(getValuesItemNames[i], out getedValues[i]);
            }
            return getedValues;
        }
        
        public void AsyncWrite(string[] writeItemNames, string[] writeItemValues)
        {
            OPCItem[] bItem = new OPCItem[writeItemNames.Length];
            for (int i = 0; i < writeItemNames.Length; i++)
            {
                for (int j = 0; j < itemNames.Count; j++)
                {
                    if (itemNames[j] == writeItemNames[i])
                    {
                        bItem[i] = opcItems.GetOPCItem(itemHandleServer[j]);
                        break;
                    }
                }
            }
            int[] temp = new int[writeItemNames.Length + 1];
            temp[0] = 0;
            for (int i = 1; i < writeItemNames.Length + 1; i++)
            {
                temp[i] = bItem[i - 1].ServerHandle;
            }
            Array serverHandles = (Array)temp;
            object[] valueTemp = new object[writeItemNames.Length + 1];
            valueTemp[0] = "";
            for (int i = 1; i < writeItemNames.Length + 1; i++)
            {
                valueTemp[i] = writeItemValues[i - 1];
            }
            Array values = (Array)valueTemp;
            Array Errors;
            int cancelID;
            opcGroup.AsyncWrite(writeItemNames.Length, ref serverHandles, ref values, out Errors, 2009, out cancelID);
            GC.Collect();
        }

        public void SyncWrite(string[] writeItemNames, string[] writeItemValues)
        {
            OPCItem[] bItem = new OPCItem[writeItemNames.Length];
            for (int i = 0; i < writeItemNames.Length; i++)
            {
                for (int j = 0; j < itemNames.Count; j++)
                {
                    if (itemNames[j] == writeItemNames[i])
                    {
                        bItem[i] = opcItems.GetOPCItem(itemHandleServer[j]);
                    }
                }
            }
            int[] temp = new int[writeItemNames.Length + 1];
            temp[0] = 0;
            for (int i = 1; i < writeItemNames.Length; i++)
            {
                temp[i] = bItem[i - 1].ServerHandle;
            }
            Array serverHandles = (Array)temp;
            object[] valueTemp = new object[writeItemNames.Length + 1];
            valueTemp[0] = "";
            for (int i = 1; i < writeItemNames.Length + 1; i++)
            {
                valueTemp[i] = writeItemValues[i - 1];
            }
            Array values = (Array)valueTemp;
            Array Errors;
            opcGroup.SyncWrite(writeItemNames.Length, ref serverHandles, ref values, out Errors);

            GC.Collect();
        }

        void opcGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            for (int i = 1; i <= NumItems; i++)
            {
                itemValues[itemNames[Convert.ToInt32(ClientHandles.GetValue(i)) - 1]] = ItemValues.GetValue(i).ToString();
            }
        }

        void opcGroup_AsyncWriteComplete(int TransactionID, int NumItems, ref Array ClientHandles, ref Array Errors)
        {
            throw new NotImplementedException();
        }
        //解構子
        public void Dispose()
        {
            if (opcGroup != null)
            {
                opcGroup.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
                opcGroup.AsyncWriteComplete -= new DIOPCGroupEvent_AsyncWriteCompleteEventHandler(opcGroup_AsyncWriteComplete);
            }
            if (opcServer != null)
            {
                opcServer.Disconnect();
                opcServer = null;
            }
            Connected = false;
        }
    }
    class XMLOperator
    {
        private static XmlDocument doc;
        private static string rootName = "Substation";
        private static string XMLfilePath;
        public XMLOperator(string subName, string host, string filePath)
        {
            doc = new XmlDocument();
            XmlElement Substation = doc.CreateElement(rootName);
            Substation.SetAttribute("Position", subName);
            Console.WriteLine(host);
            Substation.SetAttribute("Host", host);
            doc.AppendChild(Substation);
            XMLfilePath = filePath;
        }
        public void AddNode(string OPCPath)
        {
            string[] OPCNodes = OPCPath.Split('.');
            string _temp = "", path = "";
            path += rootName;
            foreach (string OPCNode in OPCNodes)
            {
                _temp = path;
                path += ("//" + OPCNode);
                XmlNode node = doc.SelectSingleNode(path);
                if (node == null)
                {
                    XmlElement newNode = doc.CreateElement(OPCNode);
                    XmlNode root = doc.SelectSingleNode(_temp);
                    root.AppendChild(newNode);
                }
            }
        }
        public void ModifyNodeValue(string OPCPath, string value)
        {
            try
            {
                string[] OPCNodes = OPCPath.Split('.');
                string _temp = "", path = "";
                path += rootName;
                foreach (string OPCNode in OPCNodes)
                {
                    _temp = path;
                    path += ("//" + OPCNode);
                }
                XmlNode node = doc.SelectSingleNode(path);
                node.InnerText = value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void SavingFile()
        {
            doc.Save(XMLfilePath);
        }
    }
    class Program
    {
        static public OPCHelper opc_client;
        static public List<string> OPCItemsNamed = new List<string>();
        static public XMLOperator xml_oper;
        static public bool _continue = true;
        static string iniFileName = "OPConfig.ini";
        static string XMLPath, ServerName, ServerHost, ConfigFileName;

        static void startOPCTask(object Interval)
        {
            string[] OPCResult = new string[OPCItemsNamed.Count()];
            while (_continue)
            {
                Console.WriteLine("---------------------------------------------------------");
                OPCResult = opc_client.GetItemValues(OPCItemsNamed.ToArray());
                for (int i = 0; i < OPCResult.Length; i++)
                {
                    xml_oper.ModifyNodeValue(OPCItemsNamed[i], OPCResult[i]);
                    Console.WriteLine("{0}: {1}", OPCItemsNamed[i], OPCResult[i]);
                }
                xml_oper.SavingFile();
                Thread.Sleep(Convert.ToInt16(Interval));
            }
        }

        static void Init()
        {
            SetupIni iniFile = new SetupIni(iniFileName);
            ServerName = iniFile.IniReadValue("SystemCfg", "ServerName");
            ServerHost = iniFile.IniReadValue("SystemCfg", "ServerHost");
            XMLPath = iniFile.IniReadValue("SystemCfg", "XMLPath");
            ConfigFileName = iniFile.IniReadValue("Customize", "FileName");
            xml_oper = new XMLOperator(ServerName, ServerHost, XMLPath);
            try
            {
                using(StreamReader sr = new StreamReader(ConfigFileName))
                {
                    string line;
                    while((line = sr.ReadLine()) != null)
                    {
                        OPCItemsNamed.Add(line);
                        xml_oper.AddNode(line);
                    }
                    xml_oper.SavingFile();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The Config File Could Not Be Read.");
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("INI File Read: {0}, {1}, {2}", ServerName, ServerHost, ConfigFileName);
        }

        static void Main(string[] args)
        {
            Init();
            opc_client = new OPCHelper("127.0.0.1", ServerName, 200);
            opc_client.AddItems(OPCItemsNamed.ToArray());
            Thread OPCService = new Thread(startOPCTask);
            OPCService.Start(200);
            Console.WriteLine("OPC Service Running...");
            while (_continue);
            Console.ReadLine();
            opc_client.Dispose();
        }
    }
}
