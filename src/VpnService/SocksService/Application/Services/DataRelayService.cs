// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Application.Services;

internal sealed class DataRelayService : IDataRelayService
{
	private readonly ILogger<DataRelayService> logger;

	public DataRelayService(ILogger<DataRelayService> logger)
	{
		this.logger = logger;
	}

	public async Task RelayAsync(IChannel clientChannel, IChannel targetChannel, TimeSpan idleTimeout,CancellationToken cancellationToken)
	{
		logger.LogInformation("Starting data relay between client and target");
		try
		{
			Task clientToTarget = RelayDataAsync(clientChannel, targetChannel, idleTimeout, cancellationToken);
			Task targetToClient = RelayDataAsync(targetChannel, clientChannel, idleTimeout, cancellationToken);

			await Task.WhenAny(clientToTarget, targetToClient);
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "An error occurred during data relay.");
		}
		finally
		{
			this.logger.LogInformation("Data relay finished. Closing channels.");
			await clientChannel.CloseAsync();
			await targetChannel.CloseAsync();
		}
	}

	private static async Task RelayDataAsync(IChannel source, IChannel destination, TimeSpan idleTimeout, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[8192]; // 8KB buffer
		while (true)
		{
			var readTask = source.ReadAsync(buffer, cancellationToken).AsTask();
			var timeoutTask = Task.Delay(idleTimeout, cancellationToken);
			var completedTask = await Task.WhenAny(readTask, timeoutTask);
			if (completedTask == timeoutTask)
			{
				// Timeout occurred
				throw new TimeoutException("Connection was idle for too long.");
			}
			int bytesRead = await readTask;
			if (bytesRead == 0)
			{
				break; // Connection closed
			}

			await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken);
		}
	}
}
