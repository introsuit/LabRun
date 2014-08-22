using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class Credentials
    {
        public string DomainName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public Credentials(string domainName, string userName, string password)
        {
            this.DomainName = domainName;
            this.UserName = userName;
            this.Password = password;
        }


    }
}
