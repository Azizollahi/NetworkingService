// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Factories;
using AG.RouterService.SocksService.Application.Abstractions.Models;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Application.Services;

internal sealed class ConnectionHandlerService : IConnectionHandler
{
	private readonly IChannelFactory channelFactory;
	private readonly IProtocolDispatcher protocolDispatcher;
	private readonly ISecureChannelFactory secureChannelFactory;
	private readonly IAccessControlService accessControlService;
	private readonly ILogger<ConnectionHandlerService> logger;

	public ConnectionHandlerService(
		IChannelFactory channelFactory,
		IProtocolDispatcher protocolDispatcher,
		ISecureChannelFactory secureChannelFactory,
		IAccessControlService accessControlService,
		ILogger<ConnectionHandlerService> logger)
	{
		this.channelFactory = channelFactory;
		this.protocolDispatcher = protocolDispatcher;
		this.secureChannelFactory = secureChannelFactory;
		this.accessControlService = accessControlService;
		this.logger = logger;
	}

	public async Task HandleConnectionAsync(ConnectionContext context, CancellationToken cancellationToken)
	{
		var remoteIp = ((IPEndPoint)context.ClientSocket.RemoteEndPoint!).Address;
		if (!await this.accessControlService.IsSourceAllowedAsync(remoteIp))
		{
			this.logger.LogWarning("Connection denied for source IP {RemoteIp} by access rules.", remoteIp);
			context.ClientSocket.Close(); // Close immediately
			return;
		}
		IChannel channel = this.channelFactory.Create(context.ClientSocket);

		try
		{
			if (context.IsSslEnabled)
			{
				channel = await secureChannelFactory.CreateSecureChannelAsync(context.Name, channel, context.Certificate!, cancellationToken);
			}

			await protocolDispatcher.DispatchAsync(channel, context.IdleTimeout, cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to handle connection. Closing channel.");
			await channel.CloseAsync();
		}
	}
}
