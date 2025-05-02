# GrpcBotProxy

A minimal IPC proxy that acts as a bridge between a Unity game and a Python bot for the card game **Scripts of Tribute**.

This is essentially a pragmatic **hack** to enable communication from Unity to a gRPC-based bot, which would otherwise be impossible since **Unity doesn't support full gRPC hosting** (e.g. `Grpc.AspNetCore`) in builds.

---

## 🧠 What is this?

Unity talks to this executable over a **Named Pipe**.  
This proxy forwards requests (like `Play`, `SelectPatron`, etc.) to a Python gRPC bot and sends back the responses to Unity.

It uses:
- `MessagePack` for high-performance, cross-platform serialization
- gRPC (via `Grpc.Net.Client`) to connect with bots
- Custom DTO mapping to simulate full game state

---

## 🚀 How to use it in Unity

1. Download the prebuilt `GrpcBotProxy.exe` from the [Releases page](https://github.com/Ematerasu/GrpcProxyHost/releases)
2. Place the `.exe` into your Unity project’s: `Assets/StreamingAssets/GrpcBotProxy.exe`
3. Unity will automatically launch the proxy when a `GrpcBot` is selected.

That's it — you're ready to play against Python bots in Unity ✨
