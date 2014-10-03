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

        private bool psychoPy;
        public bool PsychoPy
        {
            get { return psychoPy; }
            set
            {
                if (value != psychoPy)
                {
                    psychoPy = value;
                    OnPropertyChanged("PsychoPy");
                }
            }
        }

        private bool eprime { get; set; }
        public bool EPrime
        {
            get { return eprime; }
            set
            {
                if (value != eprime)
                {
                    eprime = value;
                    OnPropertyChanged("EPrime");
                }
            }
        }

        private bool ztree { get; set; }
        public bool ZTree
        {
            get { return ztree; }
            set
            {
                if (value != ztree)
                {
                    ztree = value;
                    OnPropertyChanged("ZTree");
                }
            }
        }

        private bool custom { get; set; }
        public bool Custom
        {
            get { return custom; }
            set
            {
                if (value != custom)
                {
                    custom = value;
                    OnPropertyChanged("Custom");
                }
            }
        }

        private bool chrome { get; set; }
        public bool Chrome
        {
            get { return chrome; }
            set
            {
                if (value != chrome)
                {
                    chrome = value;
                    OnPropertyChanged("Chrome");
                }
            }
        }

        private bool web { get; set; }
        public bool Web
        {
            get { return web; }
            set
            {
                if (value != web)
                {
                    web = value;
                    OnPropertyChanged("Web");
                }
            }
        }

        private bool shareScr { get; set; }
        public bool ShareScr
        {
            get { return shareScr; }
            set
            {
                if (value != shareScr)
                {
                    shareScr = value;
                    OnPropertyChanged("ShareScr");
                }
            }
        }

        private bool input { get; set; }
        public bool Input
        {
            get { return input; }
            set
            {
                if (value != input)
                {
                    input = value;
                    OnPropertyChanged("Input");
                }
            }
        }

        public int RoomNo { get; set; }
        public string ComputerName { get; set; }
        public int? BoothNo { get; set; }

public string BoothName { get; set; }

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


        //converts booth no from bridge cfg value to expected booth name value
        private string GetDisplayBoothName(int? boothNo)
        {
            string name = "";

            if (this.BoothNo == null)
            {
                return name;
            }

            if (this.BoothNo <= 24)
            {
                name = ((int)this.BoothNo) + "";
            }
            else
            {
                //start at letter before A (A is 65), so any subsequent numbers will be at range [A-*]
                int startCharValue = 64;
                int numericValue = startCharValue + ((int)this.BoothNo - 24);
                name = ((char)numericValue).ToString();
            }

            return name;
        }


        public LabClient(int RoomNo, string computerName, int? boothNo, string mac, string ip)
        {
            this.RoomNo = RoomNo;
            this.ComputerName = computerName;
            this.BoothNo = boothNo;

            this.BoothName = GetDisplayBoothName(boothNo);

            this.Mac = mac;
            this.Ip = ip;
            active = false;
            PsychoPy = false;
            EPrime = false;
            ZTree = false;
            Custom = false;
            chrome = false;
            web = false;
            input = false;
            shareScr = false;
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
