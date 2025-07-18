// Copyright By Hossein Azizollahi All Right Reserved.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Factories;

public interface ISecureChannelFactory
{
	Task<IChannel> CreateSecureChannelAsync(string name, IChannel underlyingChannel, X509Certificate2 certificate, CancellationToken cancellationToken);
}
