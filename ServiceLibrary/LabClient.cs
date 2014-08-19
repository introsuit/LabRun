﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class LabClient
    {
        public string ComputerName { get; set; }
        public int? BoothNo { get; set; }

        public LabClient(string computerName, int? boothNo)
        {
            this.ComputerName = computerName;
            this.BoothNo = boothNo;
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

        public override int GetHashCode() {
            int hash = 13;
            hash = (hash * 7) + ComputerName.GetHashCode();
            return hash;
        }
    }
}