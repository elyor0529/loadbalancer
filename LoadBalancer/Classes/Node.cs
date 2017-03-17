using System;
using System.Collections.Generic;

namespace Kvpbase
{ 
    public class Node
    {
        #region Public-Members

        public string Hostname { get; set; }
        public int Port { get; set; }
        public int Ssl { get; set; }
        public string HeartbeatUrl { get; set; }
        public int PollingIntervalMsec { get; set; }
        public DateTime? LastAttempt { get; set; }
        public DateTime? LastSuccess { get; set; }
        public DateTime? LastFailure { get; set; }
        public int MaxFailures { get; set; }
        public int? NumFailures { get; set; }
        public bool Failed { get; set; }

        #endregion

        #region Constructors-and-Factories

        public Node()
        {

        }

        #endregion
    } 
}
