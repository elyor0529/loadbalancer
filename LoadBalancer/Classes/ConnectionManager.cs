using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SyslogLogging;
using WatsonWebserver;

namespace Kvpbase
{
    public class ConnectionManager
    {
        #region Private-Members

        private LoggingModule Logging;
        private List<Connection> Connections;
        private readonly object ConnectionLock;

        #endregion

        #region Constructors-and-Factories

        public ConnectionManager(LoggingModule logging)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            Logging = logging;
            Connections = new List<Connection>();
            ConnectionLock = new object();
        }

        #endregion

        #region Public-Methods

        public void Add(int threadId, HttpRequest req)
        {
            if (threadId <= 0) return;
            if (req == null) return;

            Connection conn = new Connection();
            conn.ThreadId = threadId;
            conn.SourceIp = req.SourceIp;
            conn.SourcePort = req.SourcePort;
            conn.Method = req.Method;
            conn.RawUrl = req.RawUrlWithoutQuery;
            conn.StartTime = DateTime.Now;
            conn.EndTime = DateTime.Now;

            lock (ConnectionLock)
            {
                Connections.Add(conn);
            }
        }

        public void Close(int threadId)
        {
            if (threadId <= 0) return;

            lock (ConnectionLock)
            {
                Connections = Connections.Where(x => x.ThreadId != threadId).ToList();
            }
        }
         
        public void Update(int threadId, string hostName, string httpHostName, string nodeName)
        {
            if (threadId <= 0) return;

            lock (ConnectionLock)
            {
                Connection curr = Connections.FirstOrDefault(i => i.ThreadId == threadId);
                if (curr == null || curr == default(Connection))
                {
                    Logging.Log(LoggingModule.Severity.Warn, "Update unable to find connection on thread ID " + threadId);
                    return;
                }

                Connections.Remove(curr);
                curr.HostName = hostName;
                curr.HttpHostName = httpHostName;
                curr.NodeName = nodeName;
                Connections.Add(curr);
            }

        }

        public List<Connection> GetActiveConnections()
        {
            List<Connection> curr = new List<Connection>();

            lock (ConnectionLock)
            {
                curr = new List<Connection>(Connections);
            }

            return curr;
        }

        #endregion
    }
}
