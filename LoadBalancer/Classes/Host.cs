using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kvpbase
{
    public class Host
    {
        #region Public-Members

        public string Name { get; set; }
        public List<string> HttpHostNames { get; set; }
        public List<Node> Nodes { get; set; }
        public int LastIndex { get; set; }
        public string LoadBalancingSchema { get; set; } // roundrobin
        public string HandlingMode { get; set; }        // proxy, redirect
        public int AcceptInvalidCerts { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public Host()
        {

        }

        #endregion

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion
    }
}
