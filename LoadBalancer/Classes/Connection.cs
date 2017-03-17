using System;
using SyslogLogging;

namespace Kvpbase
{
    public class Connection
    {
        #region Public-Members

        public int ThreadId { get; set; }
        public string SourceIp { get; set; }
        public int SourcePort { get; set; }
        public string Method { get; set; }
        public string RawUrl { get; set; }

        public string HostName { get; set; }
        public string HttpHostName { get; set; }
        public string NodeName { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        #endregion

        #region Constructors-and-Factories

        public Connection()
        {

        }

        #endregion
    }
}
