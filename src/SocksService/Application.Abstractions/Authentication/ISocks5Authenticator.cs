// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Authentication;

public interface ISocks5Authenticator
{
	byte Method { get; }
	Task<AuthenticationResult> AuthenticateAsync(IChannel clientChannel, CancellationToken cancellationToken);
}
