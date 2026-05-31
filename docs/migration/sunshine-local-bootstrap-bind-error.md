# Sunshine Local Bootstrap Bind Error

## Summary

Sunshine 2.3.7 local bootstrap was failing at the Auth socket bind step with:

```txt
System.Net.Sockets.SocketException (10049):
The requested address is not valid in its context.
```

## Confirmed cause

- Runtime config source:
  `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Config.xml`
- Original values used by the external bootstrap copy:
  - `AuthIp=194.99.21.223`
  - `AuthPort=446`
  - `WorldIp=194.99.21.223`
  - `WorldPort=3467`
- Local Windows interfaces on May 28, 2026:
  - `192.168.0.187`
  - `192.168.0.195`
  - `192.168.56.1`
  - `192.168.208.1`
  - `172.16.0.2`
- The public VPS address `194.99.21.223` is not assigned to any local interface.

Because `AuthServer` and `WorldServer` were doing:

```csharp
new IPEndPoint(IPAddress.Parse(Ip), Port)
socket.Bind(endpoint)
```

Windows rejected the bind with `SocketException 10049`.

## Bind IP versus connect IP

- `AuthIp` and `WorldIp` in Sunshine are bind targets for local sockets.
- The client-facing World address comes from `sunshine.worlds.Address`.
- The client-facing Auth address comes from the Dofus client config, not from `AuthServer.Bind()`.

These are separate concerns. A public IP can be valid for clients but invalid for local socket bind.

## Fix applied

### Code

External Sunshine copy patched at:

- `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\Sunshine.AuthServer\AuthServer.cs`
- `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\Sunshine.WorldServer\WorldServer.cs`
- `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\Sunshine.BaseServer\Configuration\GameConfig.cs`
- `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\Sunshine.BaseServer\Configuration\ListenAddressResolver.cs`
- `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\Sunshine.csproj`

Behavior after the patch:

- new optional settings:
  - `AuthBindIp`
  - `WorldBindIp`
- resolver accepts:
  - `0.0.0.0`
  - `localhost`
  - a real local IPv4 assigned to a Windows interface
- resolver falls back to `0.0.0.0` when the configured bind target is invalid or non-local
- logs now distinguish real bind address from announced/configured address

Example log:

```txt
Starting IPC Auth 0.0.0.0:446 (announced as 194.99.21.223:446)
Starting IPC World 0.0.0.0:3467 (announced as 194.99.21.223:3467)
```

### Local config

External runtime config updated to:

```txt
AuthIp=194.99.21.223
AuthBindIp=0.0.0.0
AuthPort=446
WorldIp=194.99.21.223
WorldBindIp=0.0.0.0
WorldPort=3467
```

This keeps the public address visible for remote context while making the local bind safe.

## Validation

Build:

```txt
dotnet build ...\Sunshine.csproj
Result: success
```

Runtime evidence:

```txt
Starting IPC Auth 0.0.0.0:446 (announced as 194.99.21.223:446)
Starting IPC World 0.0.0.0:3467 (announced as 194.99.21.223:3467)
```

`netstat` validation with Sunshine kept alive in a normal console:

```txt
TCP    0.0.0.0:446     0.0.0.0:0     LISTENING    33392
TCP    0.0.0.0:3467    0.0.0.0:0     LISTENING    33392
```

No new `Socket.Bind` failure was produced after the patch.

## Client impact

The local server now binds correctly, but the client is still pointed at the public host:

- `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\config.xml`
  - `connection.host=194.99.21.223`
- local DB value:
  - `sunshine.worlds.Id=18`
  - `Address=194.99.21.223`
  - `Port=3467`

So local client connectivity is still pending even though the bind issue is fixed.

For a full local client test, the client host and the world address returned by Auth must point to the local machine or to a locally routable host.
