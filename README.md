# SignalRPusher
This project is used to evaluate SignalR's HubConnection performance. It supports to launch multiple HubConnection to the same SignalR server, and display the response latency.

## Build
```bash
git clone https://github.com/aspnet/SignalR.git .
cd SignalR/samples
git clone https://github.com/ccic/SignalRPusher.git
cd SignalRPusher
cd BrokerService
dotnet build
cd ../BrokerServer
dotnet build
cd ../BrokerClient
dotnet build
```
## Run
Launch broker service:
```bash
cd BrokerService
dotnet run
```
Launch server (on the same machine with BrokerService):
```
cd BrokerServer
dotnet run "http://localhost:5050/server" 4 "json"
```
Launch client (on the same machine with BrokerService):
```
cd BrokerClient
dotnet run "http://localhost:5050/client" 4000 "json"
```
## Output
When server is connected, Service side will dump:
```
Server connection number: 4
Server connection number: 2
```
When clients are connected, it will dump:
```
Client connection number: 1000
Client connection number: 2000
Client connection number: 3000
Client connection number: 4000
```
Both server and client will dump the statistics for the traffics
Server statistics:
```bash
"Latency":{"lt_400":4639,"ge_1000":61991,"lt_800":2313,"lt_300":6895,"lt_900":3905,"lt_100":2239,"lt_600":4959,"lt_700":2871,"lt_500":4851,"lt_200":4624},"ReceivedRate":99287,"ReceivedBytesRate":28488,"TotalReceivedBytes":2382888}
{"Latency":{"lt_400":4639,"ge_1000":61991,"lt_800":2313,"lt_300":6895,"lt_900":3905,"lt_100":2239,"lt_600":4959,"lt_700":2871,"lt_500":4851,"lt_200":4624},"ReceivedRate":99287,"ReceivedBytesRate":0,"TotalReceivedBytes":2382888}
{"Latency":{"lt_400":4639,"ge_1000":62298,"lt_800":2313,"lt_300":6895,"lt_900":3905,"lt_100":2239,"lt_600":4959,"lt_700":2871,"lt_500":4851,"lt_200":4624},"ReceivedRate":99594,"ReceivedBytesRate":7368,"TotalReceivedBytes":2390256}
{"Latency":{"lt_400":4639,"ge_1000":62298,"lt_800":2313,"lt_300":6895,"lt_900":3905,"lt_100":2239,"lt_600":4959,"lt_700":2871,"lt_500":4851,"lt_200":4624},"ReceivedRate":99594,"ReceivedBytesRate":0,"TotalReceivedBytes":2390256}
{"Latency":{"lt_400":4640,"ge_1000":63050,"lt_800":2313,"lt_300":6895,"lt_900":3905,"lt_100":2345,"lt_600":4959,"lt_700":2871,"lt_500":4851,"lt_200":4624},"ReceivedRate":100453,"ReceivedBytesRate":20616,"TotalReceivedBytes":2410872}
{"Latency":{"lt_400":4820,"ge_1000":68953,"lt_800":2313,"lt_300":7467,"lt_900":3905,"lt_100":2345,"lt_600":4968,"lt_700":2872,"lt_500":5018,"lt_200":5169},"ReceivedRate":107830,"ReceivedBytesRate":177048,"TotalReceivedBytes":2587920}
{"Latency":{"lt_400":4991,"ge_1000":71668,"lt_800":2313,"lt_300":7824,"lt_900":3905,"lt_100":2468,"lt_600":4972,"lt_700":2873,"lt_500":5020,"lt_200":5975},"ReceivedRate":112009,"ReceivedBytesRate":100296,"TotalReceivedBytes":2688216}

```
Client statistics:
```
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":146276,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":148272,"ReceivedBytesRate":628544,"TotalReceivedBytes":4744704}
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":147669,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":149665,"ReceivedBytesRate":44576,"TotalReceivedBytes":4789280}
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":147672,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":149668,"ReceivedBytesRate":96,"TotalReceivedBytes":4789376}
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":151632,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":153628,"ReceivedBytesRate":126720,"TotalReceivedBytes":4916096}
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":151632,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":153628,"ReceivedBytesRate":0,"TotalReceivedBytes":4916096}
{"Latency":{"lt_200":0,"lt_300":0,"lt_800":583,"ge_1000":151632,"lt_100":0,"lt_700":407,"lt_900":622,"lt_500":123,"lt_400":0,"lt_600":261},"ReceivedRate":153628,"ReceivedBytesRate":0,"TotalReceivedBytes":4916096}
```
