// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal sealed class Socks4ProtocolHandler : IProtocolHandler
	{
		private readonly ILogger<Socks4ProtocolHandler> logger;
		private readonly IOutgoingChannelFactory channelFactory;
		private readonly IDataRelayService dataRelayService;

		public Socks4ProtocolHandler(
			ILogger<Socks4ProtocolHandler> logger,
			IOutgoingChannelFactory channelFactory,
			IDataRelayService dataRelayService)
		{
			this.logger = logger;
			this.channelFactory = channelFactory;
			this.dataRelayService = dataRelayService;
		}

		public bool CanHandle(ReadOnlySpan<byte> initialBytes)
		{
			// SOCKS4 starts with version byte 0x04
			return initialBytes.Length > 0 && initialBytes[0] == Socks4Constants.Version;
		}

		public async Task HandleConnectionAsync(IChannel clientChannel, ReadOnlyMemory<byte> initialBytes, CancellationToken cancellationToken)
		{
			try
			{
				(string? host, int port) = await ReadCommandAsync(clientChannel, initialBytes, cancellationToken);
				if (host is null)
				{
					// Failed to parse, reply already sent
					return;
				}

				IChannel? targetChannel = await this.channelFactory.CreateConnectionAsync(host, port, cancellationToken);
				if (targetChannel is null)
				{
					await SendReplyAsync(clientChannel, Socks4Constants.ReplyFailed, 0, IPAddress.Any, cancellationToken);
					return;
				}

				await SendReplyAsync(clientChannel, Socks4Constants.ReplyGranted, port, IPAddress.Parse(host), cancellationToken);

				await this.dataRelayService.RelayAsync(clientChannel, targetChannel, cancellationToken);
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "Error during SOCKS4 handling.");
			}
			finally
			{
				await clientChannel.CloseAsync();
			}
		}

		private async Task<(string? host, int port)> ReadCommandAsync(IChannel channel, ReadOnlyMemory<byte> initialBytes, CancellationToken ct)
		{
			// SOCKS4 request format: [VER, CMD, DST.PORT, DST.IP, USERID, NULL]
			// We already have the VER byte from the initialBytes.

			// Read the rest of the fixed-size portion of the header
			// CMD (1) + PORT (2) + IP (4) = 7 bytes
			Memory<byte> requestBody = new byte[7];
			await channel.ReadExactlyAsync(requestBody, ct);

			// We only support CONNECT command
			if (initialBytes.Span[1] != Socks4Constants.CommandConnect)
			{
				await SendReplyAsync(channel, Socks4Constants.ReplyFailed, 0, IPAddress.Any, ct);
				return (null, 0);
			}

			int port = (requestBody.Span[0] << 8) | requestBody.Span[1];
			byte[] ipBytes = requestBody.Span.Slice(2, 4).ToArray();
			string host;

			// Read USERID until null terminator
			using (MemoryStream userIdStream = new())
			{
				byte[] singleByteBuffer = new byte[1];
				while (true)
				{
					await channel.ReadExactlyAsync(singleByteBuffer, ct);
					if (singleByteBuffer[0] == 0x00)
					{
						break; // Null terminator found
					}
					userIdStream.WriteByte(singleByteBuffer[0]);
				}
				// We don't use the UserID in this implementation, but we must consume it from the stream.
				this.logger.LogInformation("SOCKS4 UserID: {UserId}", System.Text.Encoding.ASCII.GetString(userIdStream.ToArray()));
			}
			if (ipBytes[0] == 0 && ipBytes[1] == 0 && ipBytes[2] == 0 && ipBytes[3] != 0)
			{
				logger.LogInformation("SOCKS4a request detected. Reading domain name.");
				using MemoryStream domainStream = new();
				byte[] singleByteBuffer = new byte[1];
				while (true)
				{
					await channel.ReadExactlyAsync(singleByteBuffer, ct);
					if (singleByteBuffer[0] == 0x00) break;
					domainStream.WriteByte(singleByteBuffer[0]);
				}
				host = Encoding.ASCII.GetString(domainStream.ToArray());
			}
			else
			{
				// This is a standard SOCKS4 request with an IPv4 address.
				host = new IPAddress(ipBytes).ToString();
			}
			return (host, port);
		}

		private Task SendReplyAsync(IChannel channel, byte replyCode, int port, IPAddress address, CancellationToken ct)
		{
			// SOCKS4 reply format: [VN, CD, DST.PORT, DST.IP]
			byte[] portBytes = { (byte)(port >> 8), (byte)port };
			byte[] addressBytes = address.GetAddressBytes();

			byte[] reply = new byte[8];
			reply[0] = 0x00; // VN (null byte)
			reply[1] = replyCode; // CD
			portBytes.CopyTo(reply, 2);
			addressBytes.CopyTo(reply, 4);

			return channel.WriteAsync(reply, ct).AsTask();
		}
	}
