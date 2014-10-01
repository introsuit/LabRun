using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1
{
    class Participant
    {
        public string name { get; set; }
        public string cpr { get; set; }
        public int boothno { get; set; }
        public decimal profit { get; set; }

        public Participant(string name, string cpr, int boothno, decimal profit)
        {
            this.name = name;
            this.cpr = cpr;
            this.boothno = boothno;
            this.profit = profit;
        }
    }


}
