// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.DomainNameService.Application.Abstractions.Services;
using AG.RouterService.PrivateNetwork.Application.Abstractions.Services;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Exceptions;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols.CommandHandlers;

internal sealed class ConnectCommandHandler : AbstractSocks5CommandHandler
{
	private readonly ILogger<ConnectCommandHandler> logger;
	private readonly IOutgoingChannelFactory channelFactory;
	private readonly IDataRelayService dataRelayService;
	private readonly ISocks5AddressReader addressReader;
	private readonly ISocks5ReplyWriter replyWriter;
	private readonly IAccessControlService accessControlService;
	private readonly IPrivateNetworkService privateNetworkService;
	private readonly IDnsResolverService dnsResolverService;
	public ConnectCommandHandler(
		ILogger<ConnectCommandHandler> logger,
		IOutgoingChannelFactory channelFactory,
		IDataRelayService dataRelayService,
		ISocks5AddressReader addressReader,
		ISocks5ReplyWriter replyWriter,
		IAccessControlService accessControlService, IPrivateNetworkService privateNetworkService,
		IDnsResolverService dnsResolverService)
	{
		this.logger = logger;
		this.channelFactory = channelFactory;
		this.dataRelayService = dataRelayService;
		this.addressReader = addressReader;
		this.replyWriter = replyWriter;
		this.accessControlService = accessControlService;
		this.privateNetworkService = privateNetworkService;
		this.dnsResolverService = dnsResolverService;
	}

	public override async Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken)
	{
		try
		{
			if (context.Command != Socks5Constants.CommandConnect)
			{
				await base.HandleAsync(context, cancellationToken);
				return;
			}

			this.logger.LogInformation("Handling SOCKS5 CONNECT command.");

			(string? host, int port) = await addressReader.ReadAddressAndPortAsync(context.ClientChannel, context.CommandHeader, cancellationToken);
			if (host is null)
			{
				// Error occurred and reply was already sent by the helper.
				return;
			}
			string targetHost = host;
			if (!IPAddress.TryParse(host, out _))
			{
				var privateIp = await dnsResolverService.ResolveAsync(host);
				if (privateIp.Count > 0)
				{
					logger.LogInformation("Resolved '{targetHost}' to private IP {PrivateIp} via local DNS.", targetHost, privateIp.First());
					targetHost = privateIp.First().ToString();
				}
			}

			bool isPrivateDestination = IPAddress.TryParse(targetHost, out var ipAddress) && IsPrivateIp(ipAddress);

			if (isPrivateDestination)
			{
				// For private destinations, we use the Private Network rules.
				// Requires an authenticated user.
				if (context.AuthenticatedUsername is null)
				{
					logger.LogWarning("Anonymous user attempted to connect to private IP {targetHost}. Denied.", targetHost);
					await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyConnectionNotAllowed, IPAddress.Any, 0, cancellationToken);
					return;
				}

				if (!await privateNetworkService.IsConnectionAllowedAsync(context.AuthenticatedUsername, host))
				{
					logger.LogWarning("User {User} connection to private destination {Host} denied by network rules.", context.AuthenticatedUsername, host);
					await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyConnectionNotAllowed, IPAddress.Any, 0, cancellationToken);
					return;
				}
			}
			else if (!await accessControlService.IsDestinationAllowedAsync(host))
			{
				logger.LogWarning("Connection to destination {Host} denied by access rules.", host);
				await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyConnectionNotAllowed, IPAddress.Any, 0, cancellationToken);
				return;
			}

			IChannel? targetChannel = await channelFactory.CreateConnectionAsync(targetHost, port, cancellationToken);
			if (targetChannel is null)
			{
				await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyConnectionRefused, IPAddress.Any, 0, cancellationToken);
				return;
			}

			// Ideally, we'd get the local endpoint of the targetChannel to send back to the client.
			// For now, we send a generic success reply.
			await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplySucceeded, IPAddress.Any, 0, cancellationToken);

			await dataRelayService.RelayAsync(context.ClientChannel, targetChannel, context.IdleTimeout, cancellationToken);
		}
		catch (UnsupportedAddressTypeException ex)
		{
			logger.LogWarning(ex.Message);
			await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyAddressTypeNotSupported, IPAddress.Any, 0, cancellationToken);
		}

	}
	private bool IsPrivateIp(IPAddress ipAddress)
	{
		// Basic check for common private IP ranges.
		var bytes = ipAddress.GetAddressBytes();
		switch (bytes[0])
		{
			case 10: return true;
			case 172: return bytes[1] >= 16 && bytes[1] < 32;
			case 192: return bytes[1] == 168;
			default: return false;
		}
	}
}
