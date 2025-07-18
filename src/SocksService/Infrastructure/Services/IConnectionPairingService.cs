// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net.Sockets;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Infrastructure.Listeners;

namespace AG.RouterService.SocksService.Infrastructure.Services;

public interface IConnectionPairingService
{
	// Tries to pair a new socket. Returns a full channel if its partner was already waiting.
	// Otherwise, it caches the socket and returns null.
	IChannel? TryPairConnection(Socket socket, int port, ListenerOptions options);
}
