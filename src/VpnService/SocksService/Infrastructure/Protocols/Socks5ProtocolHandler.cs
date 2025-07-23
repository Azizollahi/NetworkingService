// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Listeners;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal sealed class Socks5ProtocolHandler : IProtocolHandler
{
	private readonly ILogger<Socks5ProtocolHandler> logger;
	private readonly IEnumerable<ISocks5Authenticator> authenticators;
	private readonly IOptions<ListenerOptions> listenerOptions;
	private readonly ISocks5CommandHandler commandHandlerChain;

	public Socks5ProtocolHandler(
		ILogger<Socks5ProtocolHandler> logger,
		IEnumerable<ISocks5Authenticator> authenticators,
		IOptions<ListenerOptions> listenerOptions,
		ISocks5CommandHandler commandHandlerChain)
	{
		this.logger = logger;
		this.authenticators = authenticators;
		this.listenerOptions = listenerOptions;
		this.commandHandlerChain = commandHandlerChain;
	}

	public bool CanHandle(ReadOnlySpan<byte> initialBytes)
	{
		// SOCKS5 starts with version byte
		return initialBytes.Length > 0 && initialBytes[0] == Socks5Constants.Version;
	}

	public async Task HandleConnectionAsync(IChannel clientChannel, ReadOnlyMemory<byte> initialBytes,
		TimeSpan idleTimeout, CancellationToken cancellationToken)
	{
		try
		{
			// The handshake method now returns the result object
			AuthenticationResult authResult = await PerformAuthHandshakeAsync(clientChannel, initialBytes, cancellationToken);

			if (!authResult.IsSuccess)
			{
				this.logger.LogWarning("SOCKS5 authentication handshake failed.");
				return; // The authenticator has already sent the failure reply.
			}

			// *** CRITICAL: Use the new channel returned by the authenticator ***
			IChannel sessionChannel = authResult.Channel;

			this.logger.LogDebug("SOCKS5 authentication successful. Reading command on channel {ChannelType}.", sessionChannel.GetType().Name);

			Memory<byte> commandHeader = new byte[4];
			await sessionChannel.ReadExactlyAsync(commandHeader, cancellationToken);

			// Use the sessionChannel from here on
			var context = new Socks5CommandContext(sessionChannel, commandHeader, idleTimeout, authResult.Username);
			await this.commandHandlerChain.HandleAsync(context, cancellationToken);
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "Error during SOCKS5 handling.");
		}
		finally
		{
			await clientChannel.CloseAsync();
		}
	}

	private async Task<AuthenticationResult> PerformAuthHandshakeAsync(IChannel channel, ReadOnlyMemory<byte> initialBytes,
		CancellationToken ct)
	{
		// Read client's supported methods
		int nMethods = initialBytes.Span[1];
		var clientMethods = initialBytes.Slice(2, nMethods);

		// Filter our available authenticators to only those enabled in the configuration
		var enabledAuthenticators = this.authenticators
			.Where(a => listenerOptions.Value.AllowedAuthMethods.Contains(a.Method));

		// Find a method supported by both client and server
		ISocks5Authenticator? selectedAuthenticator = enabledAuthenticators
			.OrderBy(a => a.Method) // Optional: prefer a certain auth method
			.FirstOrDefault(a => clientMethods.Span.Contains(a.Method));

		if (selectedAuthenticator is null)
		{
			// If no compatible method is found, send 0xFF (NO ACCEPTABLE METHODS)
			byte[] noMethodReply = [Socks5Constants.Version, 0xFF];
			await channel.WriteAsync(noMethodReply, ct);
			this.logger.LogWarning("No acceptable SOCKS5 authentication method found.");
			return AuthenticationResult.Failure(channel);
		}

		// Send server choice: [VER, METHOD]
		byte[] serverReply = [Socks5Constants.Version, selectedAuthenticator.Method];
		await channel.WriteAsync(serverReply, ct);

		this.logger.LogInformation("Selected SOCKS5 authentication method: {Method}", selectedAuthenticator.Method);

		// Perform the authentication flow for the selected method
		return await selectedAuthenticator.AuthenticateAsync(channel, ct);
	}

	private async Task<(string? host, int port)> ReadCommandAsync(IChannel channel, CancellationToken ct)
	{
		try
		{
			// Format: [VER, CMD, RSV, ATYP]
			Memory<byte> requestHeader = new byte[4];
			await channel.ReadExactlyAsync(requestHeader, ct);

			if (requestHeader.Span[1] != Socks5Constants.CommandConnect)
			{
				await SendReplyAsync(channel, Socks5Constants.ReplyCommandNotSupported, ct);
				return (null, 0);
			}

			string host = "";
			byte addressType = requestHeader.Span[3];
			if (addressType == Socks5Constants.AddressTypeIPv4)
			{
				Memory<byte> addressBytes = new byte[4];
				await channel.ReadExactlyAsync(addressBytes, ct);
				host = new IPAddress(addressBytes.ToArray()).ToString();
			}
			else if (addressType == Socks5Constants.AddressTypeDomainName)
			{
				Memory<byte> lengthByte = new byte[1];
				await channel.ReadExactlyAsync(lengthByte, ct);

				Memory<byte> addressBytes = new byte[lengthByte.Span[0]];
				await channel.ReadExactlyAsync(addressBytes, ct);
				host = Encoding.ASCII.GetString(addressBytes.Span);
			}
			else if (addressType == Socks5Constants.AddressTypeIPv6)
			{
				Memory<byte> addressBytes = new byte[16];
				await channel.ReadExactlyAsync(addressBytes, ct);
				host = new IPAddress(addressBytes.ToArray()).ToString();
			}
			else
			{
				await SendReplyAsync(channel, Socks5Constants.ReplyAddressTypeNotSupported, ct);
				return (null, 0);
			}

			Memory<byte> portBytes = new byte[2];
			await channel.ReadExactlyAsync(portBytes, ct);
			int port = (portBytes.Span[0] << 8) | portBytes.Span[1];

			return (host, port);
		}
		catch (EndOfStreamException ex)
		{
			this.logger.LogWarning(ex, "Client closed connection while reading command.");
			return (null, 0);
		}
	}

	private static Task SendReplyAsync(IChannel channel, byte replyCode, CancellationToken ct)
	{
		// Format: [VER, REP, RSV, ATYP, BND.ADDR, BND.PORT]
		byte[] reply =
		{
			Socks5Constants.Version, replyCode, 0x00, // RSV
			Socks5Constants.AddressTypeIPv4, 0x00, 0x00, 0x00, 0x00, // BND.ADDR
			0x00, 0x00 // BND.PORT
		};
		return channel.WriteAsync(reply, ct).AsTask();
	}
}
