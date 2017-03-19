using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SyslogLogging;
using WatsonWebserver;
using RestWrapper;

namespace Kvpbase
{
    public partial class LoadBalancer
    {
        public static Settings _Settings;
        public static LoggingModule _Logging;
        public static HostManager _Hosts;
        public static Server _Server;
        public static ConnectionManager _Connections;
        public static ConsoleManager _Console;

        static void Main(string[] args)
        {
            #region Process-Arguments

            if (args != null && args.Length > 0)
            {
                foreach (string curr in args)
                {
                    if (curr.Equals("setup"))
                    {
                        new Setup();
                    }
                }
            } 

            #endregion

            #region Load-Config-and-Initialize

            if (!Common.FileExists("System.json"))
            {
                Setup s = new Setup();
            }

            _Settings = Settings.FromFile("System.json");

            Welcome();

            #endregion

            #region Start-Modules

            _Logging = new LoggingModule(
                _Settings.Syslog.SyslogServerIp,
                _Settings.Syslog.SyslogServerPort,
                Common.IsTrue(_Settings.Syslog.ConsoleLogging),
                (LoggingModule.Severity)(_Settings.Syslog.MinimumSeverityLevel),
                false,
                true,
                true,
                false,
                true,
                false);

            _Hosts = new HostManager(_Logging, _Settings.Hosts);
             
            _Server = new Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.Port,
                Common.IsTrue(_Settings.Server.Ssl),
                RequestHandler,
                false);

            _Connections = new ConnectionManager(_Logging);

            if (Common.IsTrue(_Settings.EnableConsole)) _Console = new ConsoleManager(_Settings, _Connections, _Hosts, ExitApplication);

            #endregion

            #region Wait-for-Server-Thread

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
            bool waitHandleSignal = false;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            } while (!waitHandleSignal);

            _Logging.Log(LoggingModule.Severity.Debug, "LoadBalancer exiting");

            #endregion 
        }

        static void Welcome()
        {
            // http://patorjk.com/software/taag/#p=display&f=Small&t=kvpbase

            string msg =
                Environment.NewLine +
                @"   _             _                    " + Environment.NewLine +
                @"  | |____ ___ __| |__  __ _ ___ ___   " + Environment.NewLine +
                @"  | / /\ V / '_ \ '_ \/ _` (_-</ -_)  " + Environment.NewLine +
                @"  |_\_\ \_/| .__/_.__/\__,_/__/\___|  " + Environment.NewLine +
                @"           |_|                        " + Environment.NewLine +
                @"                                      " + Environment.NewLine;

            Console.WriteLine(msg);
        }

        static HttpResponse RequestHandler(HttpRequest req)
        {
            DateTime startTime = DateTime.Now;
            HttpResponse resp = null;
            Host currHost = null;
            Node currNode = null;
            string hostKey = null;
            bool connAdded = false;

            try
            {
                #region Internal-APIs

                switch (req.Method.ToLower())
                {
                    case "get":
                        if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_loadbalancer/loopback", false))
                        {
                            resp = new HttpResponse(req, true, 200, null, "application/json", "Hello from LoadBalancer!", false);
                            return resp;
                        }
                        break;

                    case "put":
                    case "post":
                    case "delete":
                    default:
                        break;
                }

                #endregion

                #region Add-to-Connection-List

                _Connections.Add(Thread.CurrentThread.ManagedThreadId, req);
                connAdded = true;

                #endregion

                #region Find-Host-and-Node

                if (req.Headers.ContainsKey("Host")) hostKey = req.RetrieveHeaderValue("Host");

                if (String.IsNullOrEmpty(hostKey))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler no host header supplied for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                    resp = new HttpResponse(req, false, 400, null, "application/json", "No host header supplied", false);
                    return resp;
                }

                if (!_Hosts.SelectNodeForHost(hostKey, out currHost, out currNode))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler host or node not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                    resp = new HttpResponse(req, false, 400, null, "application/json", "Host or node not found", false);
                    return resp;
                }
                else
                {
                    if (currHost == null || currHost == default(Host))
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler host not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                        resp = new HttpResponse(req, false, 400, null, "application/json", "Host not found", false);
                        return resp;
                    }

                    if (currNode == null || currNode == default(Node))
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler node not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                        resp = new HttpResponse(req, false, 400, null, "application/json", "No node found for host", false);
                        return resp;
                    }

                    _Connections.Update(Thread.CurrentThread.ManagedThreadId, hostKey, currHost.Name, currNode.Hostname);
                }

                #endregion

                #region Process-Connection

                if (currHost.HandlingMode.Equals("redirect"))
                {
                    #region Redirect
                     
                    string redirectUrl = BuildProxyUrl(currNode, req);

                    // add host header
                    Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
                    
                    // add other headers
                    if (req.Headers != null && req.Headers.Count > 0)
                    {
                        List<string> matchHeaders = new List<string> { "host", "connection", "user-agent" };

                        foreach (KeyValuePair<string, string> currHeader in req.Headers)
                        {
                            if (matchHeaders.Contains(currHeader.Key.ToLower().Trim()))
                            {
                                continue;
                            }
                            else
                            {
                                requestHeaders.Add(currHeader.Key, currHeader.Value);
                            }
                        }
                    }

                    // process REST request
                    RestResponse restResp = RestRequest.SendRequestSafe(
                        redirectUrl,
                        req.ContentType,
                        req.Method,
                        null, null, false, 
                        Common.IsTrue(currHost.AcceptInvalidCerts),
                        requestHeaders,
                        req.Data);

                    if (restResp == null)
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler null proxy response from " + redirectUrl);
                        resp = new HttpResponse(req, false, 500, null, "application/json", "Unable to contact node", false);
                        return resp;
                    }
                    else
                    {
                        resp = new HttpResponse(req, true, restResp.StatusCode, restResp.Headers, restResp.ContentType, restResp.Data, true);
                        return resp;
                    }

                    #endregion
                }
                else if (currHost.HandlingMode.Equals("proxy"))
                {
                    #region Proxy

                    string redirectUrl = BuildProxyUrl(currNode, req);
                    
                    Dictionary<string, string> redirectHeaders = new Dictionary<string, string>();
                    redirectHeaders.Add("location", redirectUrl);

                    resp = new HttpResponse(req, true, _Settings.RedirectStatusCode, redirectHeaders, "text/plain", _Settings.RedirectStatusString, true);
                    return resp;

                    #endregion
                }
                else
                {
                    #region Unknown-Handling-Mode

                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler invalid handling mode " + currHost.HandlingMode + " for host " + currHost.Name);
                    resp = new HttpResponse(req, false, 500, null, "application/json", "Invalid handling mode '" + currHost.HandlingMode + "'", false);
                    return resp;

                    #endregion
                }

                #endregion
            }
            catch (Exception e)
            {
                _Logging.LogException("LoadBalancer", "RequestHandler", e);
                resp = new HttpResponse(req, false, 500, null, "application/json", "Internal server error", false);
                return resp;
            }
            finally
            {
                if (resp != null)
                {
                    string message = "RequestHandler " + req.SourceIp + ":" + req.SourcePort + " " + req.Method + " " + req.RawUrlWithoutQuery;
                    if (currNode != null) message += " " + hostKey + " to " + currNode.Hostname + ":" + currNode.Port + " " + currHost.HandlingMode;
                    message += " " + resp.StatusCode + " " + Common.TotalMsFrom(startTime) + "ms";
                    _Logging.Log(LoggingModule.Severity.Debug, message);
                }

                if (connAdded)
                {
                    _Connections.Close(Thread.CurrentThread.ManagedThreadId);
                }
            }
        }

        public static string BuildProxyUrl(Node redirectNode, HttpRequest req)
        { 
            UriBuilder modified = new UriBuilder(req.FullUrl);
            string ret = "";
            
            modified.Host = String.Copy(redirectNode.Hostname);
            modified.Port = redirectNode.Port;

            if (Common.IsTrue(redirectNode.Ssl)) modified.Scheme = Uri.UriSchemeHttps;
            else modified.Scheme = Uri.UriSchemeHttp;

            ret = modified.Uri.ToString();
            return ret; 
        }

        static bool ExitApplication()
        {
            _Logging.Log(LoggingModule.Severity.Info, "LoadBalancer exiting due to console request");
            Environment.Exit(0);
            return true;
        }
    }
}
