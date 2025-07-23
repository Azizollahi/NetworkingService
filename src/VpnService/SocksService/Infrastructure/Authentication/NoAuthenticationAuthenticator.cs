// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Infrastructure.Protocols;

namespace AG.RouterService.SocksService.Infrastructure.Authentication;

internal sealed class NoAuthenticationAuthenticator : ISocks5Authenticator
{
	public byte Method => Socks5Constants.AuthMethodNoAuthentication;

	public Task<AuthenticationResult> AuthenticateAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		// No action needed for this method.
		return Task.FromResult(AuthenticationResult.Success(clientChannel));
	}
}
