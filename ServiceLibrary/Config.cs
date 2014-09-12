using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class Config
    {
        public Config(string configFile)
        {
            ReadFile(configFile);
        }

        private void ReadFile(string configFile)
        {
            using (System.IO.StreamReader file = new System.IO.StreamReader(configFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith(@"#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("psychopy"))
                    {
                        Psychopy = line.Remove(0, "psychopy".Length + 1);
                    }

                    if (line.StartsWith("eprime"))
                    {
                        EPrime = line.Remove(0, "eprime".Length + 1);
                    }

                    if (line.StartsWith("ztreeadmin"))
                    {
                        Ztreeadmin = line.Remove(0, "ztreeadmin".Length + 1);
                    }

                    if (line.StartsWith("ztreeleaf"))
                    {
                        Ztreeleaf = line.Remove(0, "ztreeleaf".Length + 1);
                    }

                    if (line.StartsWith("ztreedump"))
                    {
                        Ztreedump = line.Remove(0, "ztreedump".Length + 1);
                    }


                    if (line.StartsWith("chrome"))
                    {
                        Chrome = line.Remove(0, "chrome".Length + 1);
                    }
                }
            }
        }

        public string Psychopy { get; set; }
        public string EPrime { get; set; }
        public string Ztreeadmin { get; set; }
        public string Ztreeleaf { get; set; }
        public string Ztreedump { get; set; }
        public string Chrome { get; set; }
    }
}
