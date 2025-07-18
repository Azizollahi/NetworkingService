// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Models;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Listeners;

public enum ChannelMode
{
	Standard,
	Split
}

public sealed class ChannelModeOptions
{
	public ChannelMode Client { get; set; } = ChannelMode.Standard;
}

public sealed class ClientSplitPortsOptions
{
	public int Read { get; set; }
	public int Write { get; set; }
}

public sealed class SslOptions
{
	public string CertificatePath { get; set; } = string.Empty;
	public string CertificatePassword { get; set; } = string.Empty;
}

public sealed class ListenerOptions
{
	public string Name { get; set; } = "DefaultListener";
	public string Host { get; set; } = "127.0.0.1";
	public int Port { get; set; } = 1080;
	public int Backlog { get; set; } = 100;
	public bool EnableSsl { get; set; } = false;
	public SslOptions? SslOptions { get; set; }
	public List<byte> AllowedAuthMethods { get; set; } = new();
	public bool EnableSocks4 { get; set; } = true;

	public ChannelModeOptions ChannelMode { get; set; } = new();
	public ClientSplitPortsOptions? ClientSplitPorts { get; set; }

}

internal sealed class TcpServerListener
{
	private readonly int listenPort;
	private readonly ILogger logger;
	private readonly ListenerOptions options;
	private readonly IServiceProvider serviceProvider;
	private readonly Socket listenerSocket;

	public TcpServerListener(
		int listenPort,
		ListenerOptions options,
		IServiceProvider serviceProvider,
		ILogger logger)
	{
		this.listenPort = listenPort;
		this.logger = logger;
		this.options = options;
		this.serviceProvider = serviceProvider;
		this.listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	public async Task StartAsync(CancellationToken stoppingToken)
	{
		try
		{
			IPAddress ipAddress = IPAddress.Parse(this.options.Host);
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, this.options.Port);

			this.listenerSocket.Bind(localEndPoint);
			this.listenerSocket.Listen(this.options.Backlog);

			this.logger.LogInformation("SOCKS Server is listening on {LocalEndPoint}", localEndPoint);

			while (!stoppingToken.IsCancellationRequested)
			{
				Socket clientSocket = await this.listenerSocket.AcceptAsync(stoppingToken);

				// Do not await this. This allows us to accept the next connection immediately.
				_ = Task.Run<Task>(async () =>
				{
					using var scope = this.serviceProvider.CreateScope();
					IChannel? finalChannel = null;
					if (this.options.ChannelMode.Client == ChannelMode.Standard)
					{
						// Standard flow: create a simple TcpChannel
						var channelFactory = scope.ServiceProvider.GetRequiredService<IChannelFactory>();
						finalChannel = channelFactory.Create(clientSocket);
					}
					else // Split flow
					{
						// Pass the socket to the pairing service. It will return a full SplitPortChannel
						// if the partner socket is ready, otherwise it returns null.
						var pairingService = scope.ServiceProvider.GetRequiredService<IConnectionPairingService>();
						finalChannel = pairingService.TryPairConnection(clientSocket, this.listenPort, this.options);
					}

					// If finalChannel is null, it means we are waiting for a partner connection. Do nothing.
					if (finalChannel is null) return;

					var connectionHandler = scope.ServiceProvider.GetRequiredService<IConnectionHandler>();

					X509Certificate2? certificate = null;
					if (this.options.EnableSsl)
					{
						// Load the certificate here in the Infrastructure layer
						certificate = X509CertificateLoader.LoadPkcs12FromFile(
							this.options.SslOptions!.CertificatePath,
							this.options.SslOptions.CertificatePassword,
							X509KeyStorageFlags.EphemeralKeySet);
					}

					// Create the context object
					var context = new ConnectionContext(options.Name, clientSocket, this.options.EnableSsl, certificate);
					// Pass the options for this specific listener to the handler
					await connectionHandler.HandleConnectionAsync(context, stoppingToken);
				}, stoppingToken);
			}
		}
		catch (OperationCanceledException)
		{
			// This is expected when the application is shutting down.
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "An unhandled exception occurred in the server listener.");
		}
		finally
		{
			this.logger.LogInformation("SOCKS Server is shutting down.");
			this.listenerSocket.Close();
		}
	}
}
