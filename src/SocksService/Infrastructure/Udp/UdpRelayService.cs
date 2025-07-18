// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Udp;

internal sealed class UdpRelayService : IUdpRelayService
	{
		private readonly IAccessControlService accessControlService;
		private readonly ILogger<UdpRelayService> logger;

		public UdpRelayService(IAccessControlService accessControlService,ILogger<UdpRelayService> logger)
		{
			this.accessControlService = accessControlService;
			this.logger = logger;
		}

		public Task<UdpRelayContext> StartUdpRelaySessionAsync(CancellationToken lifetimeToken)
		{
			// Create a UDP client and bind it to an available ephemeral port on all IPv4 interfaces.
			UdpClient udpListener = new UdpClient(0, AddressFamily.InterNetwork);
			IPEndPoint boundEndpoint = (IPEndPoint)udpListener.Client.LocalEndPoint!;

			this.logger.LogInformation("Started new UDP relay on {Endpoint}", boundEndpoint);

			// Start the background relay loop. This is a fire-and-forget task.
			_ = Task.Run(() => UdpRelayLoopAsync(udpListener, lifetimeToken), lifetimeToken);

			var context = new UdpRelayContext(boundEndpoint);
			return Task.FromResult(context);
		}

		private async Task UdpRelayLoopAsync(UdpClient udpListener, CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					// Wait for a UDP datagram from the client
					UdpReceiveResult result = await udpListener.ReceiveAsync(cancellationToken);
					byte[] buffer = result.Buffer;

					// Parse the SOCKS5 UDP header: [RSV, FRAG, ATYP, DST.ADDR, DST.PORT, DATA]
					// The header is at the beginning of the received data.
					if (buffer.Length < 6) continue; // Not a valid SOCKS UDP request

					// We don't support fragmentation
					if (buffer[2] != 0x00)
					{
						this.logger.LogWarning("Dropping fragmented SOCKS5 UDP packet, which is not supported.");
						continue;
					}

					(string? host, int port, int dataOffset) = ParseUdpHeader(buffer);
					if (host is null) continue;

					// **security check.**
					if (!await this.accessControlService.IsDestinationAllowedAsync(host))
					{
						this.logger.LogWarning("UDP packet to destination {Host} denied by access rules. Dropping packet.", host);
						continue; // Drop the packet
					}

					// Extract the actual application data payload
					ReadOnlyMemory<byte> payload = new ReadOnlyMemory<byte>(buffer, dataOffset, buffer.Length - dataOffset);

					// Send the payload to the final destination
					await udpListener.SendAsync(payload.ToArray(), host, port, cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				this.logger.LogInformation("UDP relay loop for {Endpoint} was canceled.", udpListener.Client.LocalEndPoint);
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "An error occurred in the UDP relay loop for {Endpoint}", udpListener.Client.LocalEndPoint);
			}
			finally
			{
				udpListener.Close();
			}
		}

		private (string? Host, int Port, int DataOffset) ParseUdpHeader(byte[] buffer)
		{
			// Parses the destination address from the SOCKS5 UDP request header.
			using (MemoryStream ms = new MemoryStream(buffer))
			{
				ms.Position = 3; // Skip RSV and FRAG
				byte addressType = (byte)ms.ReadByte();
				string? host;

				switch (addressType)
				{
					case Socks5Constants.AddressTypeIPv4:
						byte[] ipv4Bytes = new byte[4];
						ms.Read(ipv4Bytes, 0, 4);
						host = new IPAddress(ipv4Bytes).ToString();
						break;
					case Socks5Constants.AddressTypeDomainName:
						int len = ms.ReadByte();
						byte[] domainBytes = new byte[len];
						ms.Read(domainBytes, 0, len);
						host = Encoding.ASCII.GetString(domainBytes);
						break;
					case Socks5Constants.AddressTypeIPv6:
						byte[] ipv6Bytes = new byte[16];
						ms.Read(ipv6Bytes, 0, 16);
						host = new IPAddress(ipv6Bytes).ToString();
						break;
					default:
						return (null, 0, 0);
				}

				byte[] portBytes = new byte[2];
				ms.Read(portBytes, 0, 2);
				int port = (portBytes[0] << 8) | portBytes[1];

				return (host, port, (int)ms.Position);
			}
		}
	}
