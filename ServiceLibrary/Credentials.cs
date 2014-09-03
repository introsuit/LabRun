using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class Credentials
    {
        private readonly string domainName;
        public string DomainName { get { return domainName; } }

        private readonly string userName;
        public string UserName { get { return userName; } }

        private readonly string password;
        public string Password { get { return password; } }

        private readonly string domainSlashUser;
        public string DomainSlashUser { get { return domainSlashUser; } }

        private readonly string userAtDomain;
        public string UserAtDomain { get { return userAtDomain; } }

        public Credentials(string domainName, string userName, string password)
        {
            this.domainName = domainName;
            this.userName = userName;
            this.password = password;

            domainSlashUser = domainName + @"\" + userName;
            userAtDomain = userName + @"@" + domainName;
        }
    }
}
