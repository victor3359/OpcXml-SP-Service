﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da;
using TitaniumAS.Opc.Client.Da.Browsing;

namespace OpcXml_SP_Service.Libs
{
    class OpcClient
    {
        private OpcDaServer Server;
        private OpcDaGroup group;
        private DateTime localDate;
        public bool OpcIsConnected()
        {
            return Server.IsConnected;
        }
        public OpcClient(string progid, string host)
        {
            Uri url = UrlBuilder.Build(progid, host);
            try
            {
                Server = new OpcDaServer(url);
                Server.Connect();
            }
            catch (Exception ex)
            {
                localDate = DateTime.Now;
                string[] Error = new string[] { $"{localDate}\t\t{ex.Message}\n {host}/{progid} at ServerConnect." };
                File.AppendAllLines(@"./OpcConnectionError.log", Error);
                Console.WriteLine(ex.Message);
            }
            if (Server.IsConnected)
            {
                group = Server.AddGroup(@"ICPSI");
                group.IsActive = true;
            }
            else
            {
                Console.WriteLine($"Connect to Server: {progid} Failed.\nbut the service will still listening Rs232 datas.");
            }
        }
        public void AddOpcDaItems(string[] items)
        {
            if (!Server.IsConnected)
            {
                Console.WriteLine("Server connection is failed.");
                return;
            }
            List<OpcDaItemDefinition> definitions = new List<OpcDaItemDefinition>();
            foreach (string item in items)
            {
                var def = new OpcDaItemDefinition
                {
                    ItemId = item,
                    IsActive = true
                };
                definitions.Add(def);
            }
            OpcDaItemResult[] results = group.AddItems(definitions);
            foreach (OpcDaItemResult result in results)
            {
                if (result.Error.Failed)
                    Console.WriteLine($"Error adding items: {result.Error}");
            }
        }
        public OpcDaItemValue[] ReadOpcDaValues()
        {
            OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
            return values;
        }
        //Write To OpcRiver Service
        public void WriteOpcRiverDaValues(string[] items, object[] value)
        {
            List<OpcDaItem> Items = new List<OpcDaItem>();
            try
            {
                foreach (string item in items)
                {
                    Items.Add(group.Items.FirstOrDefault(x => x.ItemId == item));
                }
                group.Write(Items, value);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static void OnGroupValuesChanged(object sender, OpcDaItemValuesChangedEventArgs args)
        {
            foreach (OpcDaItemValue value in args.Values)
            {
                Console.WriteLine($"ItemId: {value.Item.ItemId}; " +
                    $"Value: {value.Value}; Quality: {value.Quality}; Timestamp: {value.Timestamp}");
            }
        }
        public void BrowseChildren(IOpcDaBrowser browser, string itemId = null, int indent = 0)
        {
            OpcDaBrowseElement[] elements = browser.GetElements(itemId);

            foreach (OpcDaBrowseElement element in elements)
            {
                Console.Write(new string(' ', indent));
                Console.WriteLine(element);

                if (!element.HasChildren)
                {
                    continue;
                }
                BrowseChildren(browser, element.ItemId, indent + 2);
            }
        }
    }
}
