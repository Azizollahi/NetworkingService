// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Application.Services;

internal sealed class ProtocolDispatcher : IProtocolDispatcher
{
	private readonly IEnumerable<IProtocolHandler> protocolHandlers;
	private readonly ILogger<ProtocolDispatcher> logger;

	public ProtocolDispatcher(IEnumerable<IProtocolHandler> protocolHandlers, ILogger<ProtocolDispatcher> logger)
	{
		this.protocolHandlers = protocolHandlers;
		this.logger = logger;
	}

	public async Task DispatchAsync(IChannel channel, TimeSpan idleTimeout, CancellationToken cancellationToken)
	{
		// Peek at the first few bytes to identify the protocol without consuming the data.
		byte[] initialBytes = new byte[8];
		int received = await channel.ReadAsync(new Memory<byte>(initialBytes), cancellationToken);

		if (received == 0)
		{
			// Connection closed before sending data.
			await channel.CloseAsync();
			return;
		}

		ReadOnlyMemory<byte> initialData = new ReadOnlyMemory<byte>(initialBytes, 0, received);

		IProtocolHandler? handler = this.protocolHandlers.FirstOrDefault(h => h.CanHandle(initialData.Span));

		if (handler is null)
		{
			this.logger.LogWarning("No protocol handler found for the connection. Closing channel.");
			await channel.CloseAsync();
			return;
		}

		this.logger.LogInformation("Dispatching connection to {HandlerName}", handler.GetType().Name);
		await handler.HandleConnectionAsync(channel, initialData, idleTimeout, cancellationToken);
	}
}
