// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Udp;

internal sealed class UdpRelaySession : UdpRelayContext
	{
		private readonly UdpClient udpClient;
		private readonly IPEndPoint clientEndpoint;
		private readonly CancellationToken lifetimeToken;
		private readonly IAccessControlService accessControlService;
		private readonly ILogger logger;

		// This acts as our NAT table, mapping a remote endpoint to the client who owns the session.
		private readonly ConcurrentDictionary<IPEndPoint, IPEndPoint> remoteToClientMap = new();

		public override IPEndPoint BoundEndpoint { get; }

		public UdpRelaySession(
			IPEndPoint clientEndpoint,
			CancellationToken lifetimeToken,
			IAccessControlService accessControlService,
			ILogger logger)
		{
			this.clientEndpoint = clientEndpoint;
			this.lifetimeToken = lifetimeToken;
			this.accessControlService = accessControlService;
			this.logger = logger;
			this.udpClient = new UdpClient(0, AddressFamily.InterNetwork);
			this.BoundEndpoint = (IPEndPoint)this.udpClient.Client.LocalEndPoint!;
		}

		public override Task StartRelayLoopAsync()
		{
			this.logger.LogInformation("Starting bi-directional UDP relay for client {Client} on {LocalEndpoint}", this.clientEndpoint, this.BoundEndpoint);
			return UdpRelayLoopAsync();
		}

		private async Task UdpRelayLoopAsync()
		{
			try
			{
				while (!this.lifetimeToken.IsCancellationRequested)
				{
					UdpReceiveResult result = await this.udpClient.ReceiveAsync(this.lifetimeToken);

					// If the packet comes from our SOCKS client, it's outbound.
					if (result.RemoteEndPoint.Equals(this.clientEndpoint))
					{
						await HandleOutboundPacketAsync(result.Buffer);
					}
					else // Otherwise, it's an inbound reply from a remote host.
					{
						await HandleInboundPacketAsync(result.Buffer, result.RemoteEndPoint);
					}
				}
			}
			catch (OperationCanceledException) { /* Expected */ }
			catch (Exception ex)
			{
				this.logger.LogError(ex, "An error occurred in the UDP relay session for client {Client}", this.clientEndpoint);
			}
		}

		private async Task HandleOutboundPacketAsync(byte[] buffer)
		{
			(string? host, int port, int dataOffset) = ParseUdpHeader(buffer);
			if (host is null) return;

			if (!await this.accessControlService.IsDestinationAllowedAsync(host))
			{
				this.logger.LogWarning("UDP packet from {Client} to destination {Host} denied by access rules. Dropping.", this.clientEndpoint, host);
				return;
			}

			var remoteEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
			this.remoteToClientMap.TryAdd(remoteEndpoint, this.clientEndpoint); // Remember this mapping

			await this.udpClient.SendAsync(buffer.AsMemory(dataOffset), remoteEndpoint, this.lifetimeToken);
		}

		private async Task HandleInboundPacketAsync(byte[] buffer, IPEndPoint remoteEndpoint)
		{
			// If we have a mapping for this remote sender, it's a reply we should forward.
			if (this.remoteToClientMap.ContainsKey(remoteEndpoint))
			{
				byte[] socksUdpPacket = WrapInSocksUdpHeader(buffer, remoteEndpoint);
				await this.udpClient.SendAsync(socksUdpPacket, this.clientEndpoint, this.lifetimeToken);
			}
			else
			{
				this.logger.LogTrace("Dropping unsolicited UDP packet from {RemoteEndpoint}", remoteEndpoint);
			}
		}

		// (ParseUdpHeader method is the same as before)
		private (string? Host, int Port, int DataOffset) ParseUdpHeader(byte[] buffer) { /* ... */ return (null, 0, 0); }

		private byte[] WrapInSocksUdpHeader(byte[] payload, IPEndPoint fromEndpoint)
		{
			// [RSV, FRAG, ATYP, SRC.ADDR, SRC.PORT, DATA]
			using (var ms = new MemoryStream())
			{
				ms.Write(new byte[] { 0x00, 0x00, 0x00 }); // RSV and FRAG
				ms.WriteByte(Socks5Constants.AddressTypeIPv4);
				ms.Write(fromEndpoint.Address.GetAddressBytes());
				ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)fromEndpoint.Port)));
				ms.Write(payload);
				return ms.ToArray();
			}
		}

		public override async ValueTask DisposeAsync()
		{
			this.logger.LogInformation("Disposing UDP relay for client {Client}", this.clientEndpoint);
			this.udpClient.Close();
			await Task.CompletedTask;
		}
	}
