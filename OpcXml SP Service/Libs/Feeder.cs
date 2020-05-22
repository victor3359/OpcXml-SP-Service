using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPC_River_Fetcher.Libs
{
    class Feeder
    {
        string[] FeederTable = {
                "C1AEB901Q101",
                "C1AEB901Q101",
                "C1AEB901Q102",
                "C1AEB901Q102",
                "C1AEB901Q103",
                "C1AEB901Q103",
                "C1AEB901Q111",
                "C1AEB901Q111",
                "C1AEB901Q112",
                "C1AEB901Q112",
                "C1AEB901Q113",
                "C1AEB901Q113",
                "C1AEB901Q114",
                "C1AEB901Q114",
                "C1AEB901Q201",
                "C1AEB901Q201",
                "C1AEB901Q202",
                "C1AEB901Q202",
                "C1AEB901Q203",
                "C1AEB901Q203",
                "C1AEB901Q204",
                "C1AEB901Q204",
                "C1AEB901Q205",
                "C1AEB901Q205",
                "C1AEB901Q206",
                "C1AEB901Q206",
                "C1AEB901Q207",
                "C1AEB901Q207",
                "C1AEB901Q208",
                "C1AEB901Q208",
                "C1AEB901Q209",
                "C1AEB901Q209",
                "C1AEB901Q210",
                "C1AEB901Q210",
                "C1AEB901Q251",
                "C1AEB901Q251",
                "C1AEB901Q252",
                "C1AEB901Q252",
                "C1AEB901Q253",
                "C1AEB901Q253",
                "C1AEB901Q254",
                "C1AEB901Q254",
                "C1AEB901Q255",
                "C1AEB901Q255",
                "C1AEB901Q256",
                "C1AEB901Q256",
                "C1AEB901Q257",
                "C1AEB901Q257",
                "C1AEB901Q258",
                "C1AEB901Q258",
                "C1AEB901Q259",
                "C1AEB901Q259",
                "C1AEB901Q260",
                "C1AEB901Q260"
            };
        string FeederName { get; set; }
        public Feeder()
        {

        }
        public bool IsFeeder(string ItemId)
        {
            foreach(string feeder in FeederTable)
            {
                if (ItemId.Contains(feeder))
                {
                    return true;
                }
            }
            return false;
        }
    }
}