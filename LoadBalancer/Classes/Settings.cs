using System;
using System.Collections.Generic;

namespace Kvpbase
{
    public class Settings
    {
        #region Public-Members

        public bool EnableConsole;
        public int RedirectStatusCode;
        public string RedirectStatusString;

        public List<Host> Hosts;
        public SettingsServer Server;
        public SettingsAuth Auth;
        public SettingsLogging Syslog;
        public SettingsRest Rest;

        #endregion

        #region Constructors-and-Factories

        public Settings()
        {

        }

        #endregion

        #region Public-Methods

        public static Settings FromFile(string filename)
        {
            return Common.DeserializeJson<Settings>(Common.ReadTextFile(filename));
        }

        #endregion
    }
    
    public class SettingsServer
    {
        #region Public-Members

        public string DnsHostname;
        public int Port;
        public bool Ssl;
        
        #endregion
    }
      
    public class SettingsAuth
    {
        #region Public-Members

        public string AdminApiKeyHeader;
        public string AdminApiKey;

        #endregion
    }

    public class SettingsLogging
    {
        #region Public-Members

        public string SyslogServerIp;
        public int SyslogServerPort;
        public int MinimumSeverityLevel;
        public bool LogRequests;
        public bool LogResponses;
        public bool ConsoleLogging;

        #endregion
    }
    
    public class SettingsRest
    {
        #region Public-Members

        public bool UseWebProxy;
        public bool AcceptInvalidCerts;
        public string WebProxyUrl;

        #endregion
    }
}
