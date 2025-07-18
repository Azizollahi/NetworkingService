// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Listeners;

internal sealed class ListenerManagerService : BackgroundService
{
	private readonly ILogger<ListenerManagerService> logger;
	private readonly IEnumerable<ListenerOptions> listenerOptions;
	private readonly IServiceProvider serviceProvider;

	public ListenerManagerService(
		ILogger<ListenerManagerService> logger,
		IOptions<List<ListenerOptions>> listenerOptions,
		IServiceProvider serviceProvider)
	{
		this.logger = logger;
		this.listenerOptions = listenerOptions.Value;
		this.serviceProvider = serviceProvider;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		this.logger.LogInformation("Starting {Count} listeners...", this.listenerOptions.Count());

		var listenerTasks = this.listenerOptions.Select(options =>
		{
			// We create a listener instance manually for each configuration
			var listener = new TcpServerListener(options, this.serviceProvider, this.logger);
			return listener.StartAsync(stoppingToken);
		}).ToList();

		await Task.WhenAll(listenerTasks);
	}
}
