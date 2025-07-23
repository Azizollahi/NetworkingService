// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IDataRelayService
{
	Task RelayAsync(IChannel clientChannel, IChannel targetChannel, TimeSpan idleTimeout, CancellationToken cancellationToken);
}
