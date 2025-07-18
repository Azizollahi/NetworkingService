// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
	private readonly IChannelFactory channelFactory;
	private readonly IOutgoingChannelFactory outgoingChannelFactory;
	private readonly IDataRelayService dataRelayService;

	public Socks4ProtocolHandler(
		ILogger<Socks4ProtocolHandler> logger,
		IChannelFactory channelFactory,
		IOutgoingChannelFactory outgoingChannelFactory,
		IDataRelayService dataRelayService)
	{
		this.logger = logger;
		this.channelFactory = channelFactory;
		this.outgoingChannelFactory = outgoingChannelFactory;
		this.dataRelayService = dataRelayService;
	}

	public bool CanHandle(ReadOnlySpan<byte> initialBytes)
	{
		return initialBytes.Length > 0 && initialBytes[0] == Socks4Constants.Version;
	}

	public async Task HandleConnectionAsync(IChannel clientChannel, ReadOnlyMemory<byte> initialBytes,
		CancellationToken cancellationToken)
	{
		try
		{
			byte command = initialBytes.Span[1];
			switch (command)
			{
				case Socks4Constants.CommandConnect:
					await HandleConnectCommandAsync(clientChannel, cancellationToken);
					break;
				case Socks4Constants.CommandBind:
					await HandleBindCommandAsync(clientChannel, cancellationToken);
					break;
				default:
					this.logger.LogWarning("Unsupported SOCKS4 command received: {Command}", command);
					await SendReplyAsync(clientChannel, Socks4Constants.ReplyFailed, 0, IPAddress.Any,
						cancellationToken);
					break;
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			this.logger.LogError(ex, "An unhandled exception occurred during SOCKS4 handling.");
		}
		finally
		{
			await clientChannel.CloseAsync();
		}
	}

	private async Task HandleConnectCommandAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		(string? host, int port) = await ReadCommandBodyAsync(clientChannel, cancellationToken);
		if (host is null) return;

		IChannel? targetChannel =
			await this.outgoingChannelFactory.CreateConnectionAsync(host, port, cancellationToken);
		if (targetChannel is null)
		{
			await SendReplyAsync(clientChannel, Socks4Constants.ReplyFailed, 0, IPAddress.Any, cancellationToken);
			return;
		}

		await SendReplyAsync(clientChannel, Socks4Constants.ReplyGranted, port, IPAddress.Any, cancellationToken);
		await this.dataRelayService.RelayAsync(clientChannel, targetChannel, cancellationToken);
	}

	private async Task HandleBindCommandAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		(string? expectedHost, int port) = await ReadCommandBodyAsync(clientChannel, cancellationToken);
		if (expectedHost is null) return;

		Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IChannel? targetChannel = null;

		try
		{
			listener.Bind(new IPEndPoint(IPAddress.Any, 0));
			listener.Listen(1);
			IPEndPoint listeningEndpoint = (IPEndPoint)listener.LocalEndPoint!;
			this.logger.LogInformation("SOCKS4 BIND: Listening for incoming connection on {Endpoint}", listeningEndpoint);

			// Send the FIRST reply with the listening port and IP.
			await SendReplyAsync(clientChannel, Socks4Constants.ReplyGranted, listeningEndpoint.Port, listeningEndpoint.Address, cancellationToken);

			Socket targetSocket = await listener.AcceptAsync(cancellationToken);
			IPEndPoint remoteEndpoint = (IPEndPoint)targetSocket.RemoteEndPoint!;
			this.logger.LogInformation("SOCKS4 BIND: Accepted connection from {RemoteEndpoint}", remoteEndpoint);

			// **This is the new security check.**
			// Verify that the connecting IP matches the one the client expected.
			if (remoteEndpoint.Address.ToString() != expectedHost)
			{
				this.logger.LogWarning("SOCKS4 BIND security check failed. Expected connection from {ExpectedHost} but received from {ActualHost}.", expectedHost, remoteEndpoint.Address);
				await SendReplyAsync(clientChannel, Socks4Constants.ReplyFailed, 0, IPAddress.Any, cancellationToken);
				targetSocket.Close();
				return;
			}

			// Send the SECOND reply to confirm the connection.
			await SendReplyAsync(clientChannel, Socks4Constants.ReplyGranted, listeningEndpoint.Port, listeningEndpoint.Address, cancellationToken);

			targetChannel = this.channelFactory.Create(targetSocket);
			await this.dataRelayService.RelayAsync(clientChannel, targetChannel, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			this.logger.LogError(ex, "SOCKS4 BIND command failed.");
			if (clientChannel.IsConnected)
			{
				await SendReplyAsync(clientChannel, Socks4Constants.ReplyFailed, 0, IPAddress.Any, cancellationToken);
			}
		}
		finally
		{
			listener.Close();
			if (targetChannel is not null) await targetChannel.CloseAsync();
		}
	}

	private async Task<(string? host, int port)> ReadCommandBodyAsync(IChannel channel, CancellationToken ct)
	{
		// Reads the part of the request after VER and CMD: [DST.PORT, DST.IP, USERID, NULL]
		Memory<byte> requestBody = new byte[6];
		await channel.ReadExactlyAsync(requestBody, ct);

		int port = (requestBody.Span[0] << 8) | requestBody.Span[1];
		var ipBytes = requestBody.Span.Slice(2, 4).ToArray();
		string host;

		// Consume the UserID field until the NULL terminator.
		using (MemoryStream userIdStream = new MemoryStream())
		{
			byte[] singleByteBuffer = new byte[1];
			while (true)
			{
				await channel.ReadExactlyAsync(singleByteBuffer, ct);
				if (singleByteBuffer[0] == 0x00) break;
				userIdStream.WriteByte(singleByteBuffer[0]);
			}

			this.logger.LogDebug("SOCKS4 UserID: {UserId}", Encoding.ASCII.GetString(userIdStream.ToArray()));
		}

		// SOCKS4a check
		if (ipBytes[0] == 0 && ipBytes[1] == 0 && ipBytes[2] == 0 && ipBytes[3] != 0)
		{
			// Domain name follows the UserID's null byte
			using (MemoryStream domainStream = new MemoryStream())
			{
				byte[] singleByteBuffer = new byte[1];
				while (true)
				{
					await channel.ReadExactlyAsync(singleByteBuffer, ct);
					if (singleByteBuffer[0] == 0x00) break;
					domainStream.WriteByte(singleByteBuffer[0]);
				}

				host = Encoding.ASCII.GetString(domainStream.ToArray());
			}

			this.logger.LogInformation("SOCKS4a request parsed for domain: {Host}", host);
		}
		else
		{
			host = new IPAddress(ipBytes).ToString();
		}

		return (host, port);
	}

	private static Task SendReplyAsync(IChannel channel, byte replyCode, int port, IPAddress address,
		CancellationToken ct)
	{
		byte[] portBytes = { (byte)(port >> 8), (byte)port };
		byte[] addressBytes = address.GetAddressBytes();

		byte[] reply = new byte[8];
		reply[0] = 0x00; // VN is always null.
		reply[1] = replyCode;
		portBytes.CopyTo(reply, 2);
		addressBytes.CopyTo(reply, 4);

		return channel.WriteAsync(reply, ct).AsTask();
	}
}
