using System;
using System.Xml;

namespace OPC_River_Fetcher.Libs
{
    class XmlOperator
    {
        public XmlDocument doc { get; }
        private static string rootName = @"PowerPlant";
        private static string XMLfilePath;
        public XmlOperator(string subName, string host, string filePath)
        {
            doc = new XmlDocument();
            XmlElement Substation = doc.CreateElement(rootName);
            Substation.SetAttribute(@"Name", subName);
            Substation.SetAttribute(@"Host", host);
            doc.AppendChild(Substation);
            XMLfilePath = filePath;
        }
        private string ReDefinitionPath(string root)
        {
            return root.Replace(@"$", @"s33b6a").Replace(@":", @"s33b6b").Replace(@"/", @"s33b6c").Replace(@"@", @"s33b6d").Replace(@"_", @"s33b6e").Replace(@"-", @"s33b6f").Replace(@"+", @"s33b6g");
        }
        public void AddNode(string OPCPath)
        {
            OPCPath = ReDefinitionPath(OPCPath);
            string _temp = "", path = "";
            path += rootName;
            _temp = path;
            path += ($"//{OPCPath}");
            XmlNode node = doc.SelectSingleNode(path);
            if (node == null)
            {
                XmlElement newNode = doc.CreateElement(OPCPath);
                XmlNode root = doc.SelectSingleNode(_temp);
                root.AppendChild(newNode);
            }
        }
        public void ModifyNodeValue(string OPCPath, string value)
        {
            try
            {
                switch (value)
                {
                    case @"True":
                        value = @"1";
                        break;
                    case @"False":
                        value = @"0";
                        break;
                    default:
                        break;
                }
                OPCPath = ReDefinitionPath(OPCPath);
                string _temp = "", path = "";
                path += rootName;
                _temp = path;
                path += ($"//{OPCPath}");
                XmlNode node = doc.SelectSingleNode(path);
                node.InnerText = value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void StringToSavingFile(string xs)
        {
            if (String.IsNullOrEmpty(xs)) return;
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xs);

                xml.Save(XMLfilePath);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SavingFile()
        {
            doc.Save(XMLfilePath);
        }
    }
}
