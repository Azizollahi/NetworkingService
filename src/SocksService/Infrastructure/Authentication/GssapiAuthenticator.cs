// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Infrastructure.Channels;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Authentication;

internal sealed class GssapiAuthenticator : ISocks5Authenticator
{
	private readonly ILogger<GssapiAuthenticator> logger;
	public byte Method => 0x01; // GSS-API Method ID

	public GssapiAuthenticator(ILogger<GssapiAuthenticator> logger)
	{
		this.logger = logger;
	}

	public async Task<AuthenticationResult> AuthenticateAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		// We must leave the underlying stream open for the new SslChannel to use after the handshake.
		var channelStream = new ChannelStream(clientChannel);
		var negotiateStream = new NegotiateStream(channelStream, leaveInnerStreamOpen: true);

		try
		{
			await negotiateStream.AuthenticateAsServerAsync();

			if (negotiateStream.IsAuthenticated)
			{
				this.logger.LogInformation(
					"GSS-API authentication successful for user {UserName} using {AuthType}. IsEncrypted: {IsEncrypted}",
					negotiateStream.RemoteIdentity.Name,
					negotiateStream.RemoteIdentity.AuthenticationType,
					negotiateStream.IsEncrypted);

				// Authentication succeeded. We return a *new* channel that wraps the now-encrypted stream.
				// The SslChannel can wrap any Stream, including NegotiateStream.
				var secureChannel = new StreamBasedChannel(negotiateStream, clientChannel.RemoteEndPoint);
				return AuthenticationResult.Success(secureChannel);
			}

			this.logger.LogWarning("GSS-API authentication completed but the session is not authenticated.");
			return AuthenticationResult.Failure(clientChannel);
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "GSS-API authentication failed with an exception.");
			await negotiateStream.DisposeAsync(); // Clean up the stream on failure.
			return AuthenticationResult.Failure(clientChannel);
		}
	}
}
