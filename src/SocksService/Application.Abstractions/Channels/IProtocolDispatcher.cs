// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Channels;

public interface IProtocolDispatcher
{
	Task DispatchAsync(IChannel channel, CancellationToken cancellationToken);
}
