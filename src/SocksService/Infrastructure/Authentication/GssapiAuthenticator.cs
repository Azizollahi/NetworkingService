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

	public async Task<bool> AuthenticateAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		// NegotiateStream requires a System.IO.Stream, so we use our adapter.
		var channelStream = new ChannelStream(clientChannel);

		// We wrap the base stream in a NegotiateStream.
		// `leaveInnerStreamOpen` is set to true because we want to continue using the channel
		// after authentication, and we don't want the SslStream to close it.
		await using (var negotiateStream = new NegotiateStream(channelStream, leaveInnerStreamOpen: true))
		{
			try
			{
				// This call handles the entire multi-stage Kerberos/NTLM token exchange.
				await negotiateStream.AuthenticateAsServerAsync();

				if (negotiateStream.IsAuthenticated)
				{
					logger.LogInformation(
						"GSS-API authentication successful for user {UserName} using {AuthType}.",
						negotiateStream.RemoteIdentity.Name,
						negotiateStream.RemoteIdentity.AuthenticationType);

					// Note: After authentication, negotiateStream.IsEncrypted and IsSigned will be true.
					// A full implementation would replace the original channel with a new one that
					// uses this stream to provide encryption for the rest of the session.
					// For now, we will proceed with the unencrypted underlying channel.

					return true;
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "GSS-API authentication failed.");
				return false;
			}
		}

		logger.LogWarning("GSS-API authentication failed for an unknown reason.");
		return false;
	}
}
