# Kvpbase Loadbalancer

A simple high-availability loadbalancer written in C# using Watson.

![alt tag](https://github.com/maraudersoftware/LoadBalancer/blob/master/assets/diagram.png)

## Setup
Run the app with ```setup``` in the command line arguments to run the setup script.

## Definitions
The loadbalancer currently supports round-robin load-balancing.  Nodes are polled according to their configuration, and removed from rotation when the maximum number of failures are reached.  Polling will continue while a node is failed to detect return-to-service conditions.  

A 'Host' is defined as a resource that maps to multiple physical 'Nodes'.  Hosts are defined by the values that could be found in the HTTP host header of incoming requests.  Nodes mapping to hosts are defined by their hostname and TCP port.

The ```HeartbeatUrl``` must be a full URL including the protocol.  

```Mode``` can either be set to 'proxy' or 'redirect'.  In the case of proxy, the loadbalancer will submit a request on behalf of the requestor and marshal the response back to the requestor.  In the case of redirect, the loadbalancer will send an HTTP redirect according to the configuration.

## Sample Configuration
```
{
  "EnableConsole": 1,
  "BalanceScheme": "roundrobin",
  "HeartbeatIntervalSec": 5,
  "RedirectStatusCode": 302,
  "RedirectStatusString": "Moved Temporarily",
  "Hosts": [
    {
      "Name": "MyApp",
      "HttpHostNames": [
        "www.myapp.com",
        "myapp.com"
      ],
      "Nodes": [
        {
          "Hostname": "10.1.1.1",
          "Port": 80,
          "Ssl": 0,
          "HeartbeatUrl": "http://10.1.1.1:80/loopback",
          "PollingIntervalMsec": 2500,
          "MaxFailures": 4,
          "Failed": false
        },
        {
          "Hostname": "10.1.1.2",
          "Port": 80,
          "Ssl": 0,
          "HeartbeatUrl": "http://10.1.1.2:80/loopback",
          "PollingIntervalMsec": 2500,
          "MaxFailures": 4,
          "Failed": false
        }
      ],
      "LastIndex": 0,
      "LoadBalancingSchema": "roundrobin",
      "HandlingMode": "redirect",
      "AcceptInvalidCerts": 1
    }
  ],
  "Server": {
    "DnsHostname": "+",
    "Port": 9000,
    "Ssl": 0
  },
  "Syslog": {
    "SyslogServerIp": "127.0.0.1",
    "SyslogServerPort": 514,
    "MinimumSeverityLevel": 1,
    "LogRequests": 0,
    "LogResponses": 0,
    "ConsoleLogging": 1
  },
  "Rest": {
    "UseWebProxy": 0,
    "WebProxyUrl": "",
    "AcceptInvalidCerts": 1
  }
}

```

## Running under Mono
Watson works well in Mono environments to the extent that we have tested it. It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

NOTE: Windows accepts '0.0.0.0' as an IP address representing any interface.  On Mac and Linux with Mono you must supply a specific IP address ('127.0.0.1' is also acceptable, but '0.0.0.0' is NOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server LoadBalancer.exe
mono --server myapp.exe
```
