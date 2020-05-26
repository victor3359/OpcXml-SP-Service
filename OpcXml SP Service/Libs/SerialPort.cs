using System;
using System.IO;
using System.IO.Ports;
using System.Xml;

namespace OpcXml_SP_Service.Libs
{
    class serialPort
    {
        private SerialPort port;
        private string data;
        private string tmpStr = @"<Data>", tmpEnd = @"</Data>";
        private DateTime localDate;
        private string ReceivedFilePath;
        public XmlDocument xml = new XmlDocument();
        public bool hasNewData;
        public serialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, string ReceivedFilePath)
        {
            try
            {
                this.ReceivedFilePath = ReceivedFilePath;
                port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                port.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);
                port.Open();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                localDate = DateTime.Now;
                string[] Error = new string[] { $"{localDate}\t\t{ex.Message}" };
                File.AppendAllLines(@"./SPConnectionError.log", Error);
            }
        }
        public bool IsOpened()
        {
            return port.IsOpen;
        }
        public void sendDatas(string senddata)
        {
            try
            {
                localDate = DateTime.Now;
                tmpStr = $"<Data UpdateTime=\"{ localDate.ToString(System.Globalization.CultureInfo.InvariantCulture) }\">";
                string buf = tmpStr + senddata + tmpEnd;
                if (port.IsOpen)
                {
                    Console.WriteLine($"Writing... \n{buf}");
                    port.Write(buf);
                }
            } 
            catch (Exception ex)
            {
                localDate = DateTime.Now;
                Console.WriteLine(ex.Message);
                string[] Error = new string[] { $"{localDate}\t\t{ex.Message}" };
                File.AppendAllLines(@"./SPConnectionError.log", Error);
            }
        }
        private void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            data = "";
            data += $"{ port.ReadTo(tmpEnd) }{tmpEnd}";
            Console.WriteLine($"Received:\n{data}");
            try
            {
                xml.LoadXml(data);
                xml.Save(ReceivedFilePath);
                hasNewData = true;
            }
            catch(Exception ex)
            {
                localDate = DateTime.Now;
                string[] Error = new string[] { $"{localDate}\t\t{ex.Message}" };
                File.AppendAllLines(@"./SPConnectionError.log", Error);
                Console.WriteLine(ex.Message);
            }
        }
    }
}
