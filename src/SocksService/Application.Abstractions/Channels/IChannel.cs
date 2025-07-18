// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Channels;

public interface IChannel
{
	bool IsConnected { get; }
	ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
	ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
	ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
	Task CloseAsync();
}
