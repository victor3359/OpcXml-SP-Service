using System;
using System.IO.Ports;
using System.Xml;

namespace OPC_River_Fetcher.Libs
{
    class serialPort
    {
        private SerialPort port;
        public bool isConnected = false;
        public string data { get; set; }
        private string tmpStr = @"<Data>", tmpEnd = @"</Data>";
        private DateTime localDate;
        private string ReceivedFilePath;
        private XmlDocument xml { get; set; }
        public serialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, string ReceivedFilePath)
        {
            try
            {
                this.ReceivedFilePath = ReceivedFilePath;
                port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                port.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);
                port.Open();
                isConnected = true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                isConnected = false;
            }
        }
        public void sendDatas(string senddata)
        {
            try
            {
                localDate = DateTime.Now;
                tmpStr = $"<Data UpdateTime=\"{ localDate }\">";
                string buf = tmpStr + senddata + tmpEnd;
                if (isConnected)
                {
                    Console.WriteLine($"Writing... \n{buf}");
                    port.Write(buf);
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                isConnected = false;
            }
        }
        private void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            data += $"{ port.ReadTo(tmpEnd) }{tmpEnd}";
            Console.WriteLine($"Received: {data}");
            xml.LoadXml(data);
            xml.Save(ReceivedFilePath);
        }
    }
}
