# SignalRPusher
This project is used to evaluate SignalR's HubConnection performance. It supports to launch multiple HubConnection to the same SignalR server, and display the response latency.

## Build
```bash
git clone https://github.com/aspnet/SignalR.git .
cd SignalR/samples
git clone https://github.com/ccic/SignalRPusher.git
cd SignalRPusher
cd PushServer
dotnet build
cd ../PushClient
dotnet build
```
## Run
Launch server:
```bash
cd PushServer
dotnet run
```
Launch client (on the same machine with PushServer):
```
cd PushClient
dotnet run "http://localhost:5050" 30 3000 "json"
```
## Output
```bash
{"Latency":{"lt_100":0,"ge_500":0,"lt_200":0,"lt_300":0,"lt_400":0},"ReceivedRate":0,"TotalReceivedBytes":0}
{"Latency":{"lt_100":3542,"ge_500":100,"lt_200":2193,"lt_300":463,"lt_400":418},"ReceivedRate":53728,"TotalReceivedBytes":53728}
{"Latency":{"lt_100":3542,"ge_500":3876,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":32688,"TotalReceivedBytes":86416}
{"Latency":{"lt_100":3542,"ge_500":15204,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":90624,"TotalReceivedBytes":177040}
{"Latency":{"lt_100":3542,"ge_500":27144,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":95520,"TotalReceivedBytes":272560}
{"Latency":{"lt_100":3542,"ge_500":39573,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":99432,"TotalReceivedBytes":371992}
{"Latency":{"lt_100":3542,"ge_500":54789,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":121728,"TotalReceivedBytes":493720}
{"Latency":{"lt_100":3542,"ge_500":60615,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":46608,"TotalReceivedBytes":540328}
{"Latency":{"lt_100":3542,"ge_500":67482,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":54936,"TotalReceivedBytes":595264}
{"Latency":{"lt_100":3542,"ge_500":73374,"lt_200":2193,"lt_300":463,"lt_400":728},"ReceivedRate":47136,"TotalReceivedBytes":642400}
```
