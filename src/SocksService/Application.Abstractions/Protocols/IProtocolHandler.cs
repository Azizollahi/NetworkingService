// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Protocols;

public interface IProtocolHandler
{
	bool CanHandle(ReadOnlySpan<byte> initialBytes);
	Task HandleConnectionAsync(IChannel clientChannel, ReadOnlyMemory<byte> initialBytes, CancellationToken cancellationToken);
}
