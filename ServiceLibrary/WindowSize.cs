using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class WindowSize
    {
        public string Name { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        public WindowSize(string name, int? width, int? height)
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
