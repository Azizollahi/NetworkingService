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
		var listenerTasks = new List<Task>();

		foreach (var options in this.listenerOptions)
		{
			if (options.ChannelMode.Client == ChannelMode.Standard)
			{
				this.logger.LogInformation("Starting STANDARD listener '{Name}' on {Host}:{Port}", options.Name, options.Host, options.Port);
				var listener = new TcpServerListener(options.Port, options, this.serviceProvider, this.logger);
				listenerTasks.Add(listener.StartAsync(stoppingToken));
			}
			else // ChannelMode is Split
			{
				this.logger.LogInformation("Starting SPLIT listener '{Name}' on READ:{ReadPort} and WRITE:{WritePort}", options.Name, options.ClientSplitPorts!.Read, options.ClientSplitPorts.Write);
				var readListener = new TcpServerListener(options.ClientSplitPorts.Read, options, this.serviceProvider, this.logger);
				var writeListener = new TcpServerListener(options.ClientSplitPorts.Write, options, this.serviceProvider, this.logger);
				listenerTasks.Add(readListener.StartAsync(stoppingToken));
				listenerTasks.Add(writeListener.StartAsync(stoppingToken));
			}
		}

		await Task.WhenAll(listenerTasks);
	}
}
