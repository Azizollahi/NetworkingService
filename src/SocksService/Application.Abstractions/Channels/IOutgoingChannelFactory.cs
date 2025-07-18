// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Channels;

public interface IOutgoingChannelFactory
{
	Task<IChannel?> CreateConnectionAsync(string host, int port, CancellationToken cancellationToken);
}
