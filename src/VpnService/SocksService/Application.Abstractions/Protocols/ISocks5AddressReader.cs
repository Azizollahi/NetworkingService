// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Protocols;

public interface ISocks5AddressReader
{
	Task<(string? Host, int Port)> ReadAddressAndPortAsync(
		IChannel clientChannel,
		ReadOnlyMemory<byte> commandHeader,
		CancellationToken cancellationToken);
}
