using System;
using System.Xml;
using System.Text.RegularExpressions;

namespace OpcXml_SP_Service.Libs
{
    class XmlOperator
    {
        public XmlDocument doc { get; }
        private string rootName = @"PowerPlant";
        private string XmlFilePath;
        Regex regNum = new Regex("^[0-9]");
        public XmlOperator(string subName, string host, string filePath)
        {
            doc = new XmlDocument();
            XmlElement Substation = doc.CreateElement(rootName);
            Substation.SetAttribute(@"Name", subName);
            Substation.SetAttribute(@"Host", host);
            doc.AppendChild(Substation);
            XmlFilePath = filePath;
        }
        private string ReDefinitionPath(string root)
        {
            if (regNum.IsMatch(root)) root = $"i{root}";
            return root.Replace(@"$", @"s3b1").Replace(@":", @"s3b2").Replace(@"/", @"s3b3").Replace(@"@", @"s3b4").Replace(@"_", @"s3b5").Replace(@"-", @"s3b6").Replace(@"+", @"s3b7").Replace(@"#", "s3b8");
        }
        public void AddNode(string OpcPath)
        {
            OpcPath = ReDefinitionPath(OpcPath);
            string path = $"{rootName}//{OpcPath}";
            XmlNode node = doc.SelectSingleNode(path);
            if (node == null)
            {
                XmlElement newNode = doc.CreateElement(OpcPath);
                XmlNode root = doc.SelectSingleNode(rootName);
                root.AppendChild(newNode);
            }
        }
        public void ModifyNodeValue(string OpcPath, string value)
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
                OpcPath = ReDefinitionPath(OpcPath);
                string path = $"{rootName}//{OpcPath}";
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

                xml.Save(XmlFilePath);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SavingFile()
        {
            Console.WriteLine($"\nSaving File: {XmlFilePath}.\n");
            doc.Save(XmlFilePath);
        }
    }
}
