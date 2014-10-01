using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    public class CustomRunTestApp : TestApp
    {
        public CustomRunTestApp(List<string> extensions)
            : base("Custom Run")
        {
            foreach (String ext in extensions) {
                resultExts.Add(ext);

            }
        }
    }
}
