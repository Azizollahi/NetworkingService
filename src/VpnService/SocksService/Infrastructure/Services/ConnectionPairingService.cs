// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Infrastructure.Channels;
using AG.RouterService.SocksService.Infrastructure.Listeners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Services;

// A simple DTO to hold a waiting socket and its timestamp.
internal class WaitingSocket
	{
		public Socket Socket { get; }
		public DateTime ReceivedAt { get; }
		public WaitingSocket(Socket socket)
		{
			this.Socket = socket;
			this.ReceivedAt = DateTime.UtcNow;
		}
	}

	internal sealed class ConnectionPairingService : IConnectionPairingService, IHostedService
	{
		private readonly ILogger<ConnectionPairingService> logger;
		private readonly ConcurrentDictionary<IPAddress, WaitingSocket> waitingReadSockets = new();
		private readonly ConcurrentDictionary<IPAddress, WaitingSocket> waitingWriteSockets = new();
		private readonly Timer cleanupTimer;

		public ConnectionPairingService(ILogger<ConnectionPairingService> logger)
		{
			this.logger = logger;
			// A background timer to clean up old, unpaired sockets to prevent memory leaks.
			this.cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
		}

		public IChannel? TryPairConnection(Socket socket, int listenPort, ListenerOptions options)
		{
			var remoteIp = ((IPEndPoint)socket.RemoteEndPoint!).Address;
			bool isReadSocket = (listenPort == options.ClientSplitPorts!.Read);

			if (isReadSocket)
			{
				// This is a Read socket. Look for its Write partner.
				if (this.waitingWriteSockets.TryRemove(remoteIp, out WaitingSocket? partner))
				{
					this.logger.LogInformation("Paired READ socket from {IP} with waiting WRITE socket.", remoteIp);
					return new SplitPortChannel(readSocket: socket, writeSocket: partner.Socket);
				}
				else
				{
					this.logger.LogDebug("Caching READ socket from {IP}, waiting for WRITE partner.", remoteIp);
					this.waitingReadSockets[remoteIp] = new WaitingSocket(socket);
					return null;
				}
			}
			else // This is a Write socket
			{
				if (this.waitingReadSockets.TryRemove(remoteIp, out WaitingSocket? partner))
				{
					this.logger.LogInformation("Paired WRITE socket from {IP} with waiting READ socket.", remoteIp);
					return new SplitPortChannel(readSocket: partner.Socket, writeSocket: socket);
				}
				else
				{
					this.logger.LogDebug("Caching WRITE socket from {IP}, waiting for READ partner.", remoteIp);
					this.waitingWriteSockets[remoteIp] = new WaitingSocket(socket);
					return null;
				}
			}
		}

		private void Cleanup(object? state)
		{
			var cutoff = DateTime.UtcNow.AddSeconds(-5); // Remove sockets older than 5 seconds
			foreach (var entry in this.waitingReadSockets)
			{
				if (entry.Value.ReceivedAt < cutoff)
				{
					if (this.waitingReadSockets.TryRemove(entry.Key, out var oldSocket))
					{
						this.logger.LogWarning("Cleaning up stale READ socket from {IP}", entry.Key);
						oldSocket.Socket.Close();
					}
				}
			}
			// Repeat for waitingWriteSockets
		}

		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopAsync(CancellationToken cancellationToken)
		{
			this.cleanupTimer.Dispose();
			return Task.CompletedTask;
		}
	}
