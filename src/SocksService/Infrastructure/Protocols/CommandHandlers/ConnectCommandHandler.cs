// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
	public ConnectCommandHandler(
		ILogger<ConnectCommandHandler> logger,
		IOutgoingChannelFactory channelFactory,
		IDataRelayService dataRelayService,
		ISocks5AddressReader addressReader,
		ISocks5ReplyWriter replyWriter)
	{
		this.logger = logger;
		this.channelFactory = channelFactory;
		this.dataRelayService = dataRelayService;
		this.addressReader = addressReader;
		this.replyWriter = replyWriter;
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

			IChannel? targetChannel = await this.channelFactory.CreateConnectionAsync(host, port, cancellationToken);
			if (targetChannel is null)
			{
				await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyConnectionRefused, IPAddress.Any, 0, cancellationToken);
				return;
			}

			// Ideally, we'd get the local endpoint of the targetChannel to send back to the client.
			// For now, we send a generic success reply.
			await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplySucceeded, IPAddress.Any, 0, cancellationToken);

			await this.dataRelayService.RelayAsync(context.ClientChannel, targetChannel, cancellationToken);
		}
		catch (UnsupportedAddressTypeException ex)
		{
			this.logger.LogWarning(ex.Message);
			await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyAddressTypeNotSupported, IPAddress.Any, 0, cancellationToken);
		}

	}
}
