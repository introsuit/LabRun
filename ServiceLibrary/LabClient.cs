using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class LabClient : INotifyPropertyChanged
    {
        private bool active;
        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    active = value;
                    OnPropertyChanged("Active");
                }
            }
        }

        public bool PsychoPy { get; set; }
        public bool EPrime { get; set; }
        public bool ZTree { get; set; }
        public bool Chrome { get; set; }

        public int RoomNo { get; set; }
        public string ComputerName { get; set; }
        public int? BoothNo { get; set; }
        public string Mac { get; set; }
        public string Ip { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }

        public LabClient(int RoomNo, string computerName, int? boothNo, string mac, string ip)
        {
            this.RoomNo = RoomNo;
            this.ComputerName = computerName;
            this.BoothNo = boothNo;
            this.Mac = mac;
            this.Ip = ip;
            active = false;
            PsychoPy = false;
            EPrime = false;
            ZTree = false;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            LabClient p = obj as LabClient;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (ComputerName == p.ComputerName);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + ComputerName.GetHashCode();
            return hash;
        }    
    }
}
