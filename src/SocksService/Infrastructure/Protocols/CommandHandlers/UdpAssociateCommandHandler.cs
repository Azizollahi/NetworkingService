// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols.CommandHandlers;

internal sealed class UdpAssociateCommandHandler : AbstractSocks5CommandHandler
	{
		private readonly ILogger<UdpAssociateCommandHandler> logger;
		private readonly IUdpRelayService udpRelayService;
		private readonly ISocks5ReplyWriter replyWriter;

		public UdpAssociateCommandHandler(
			ILogger<UdpAssociateCommandHandler> logger,
			IUdpRelayService udpRelayService,
			ISocks5ReplyWriter replyWriter)
		{
			this.logger = logger;
			this.udpRelayService = udpRelayService;
			this.replyWriter = replyWriter;
		}

		public override async Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken)
		{
			if (context.Command != Socks5Constants.CommandUdpAssociate)
			{
				await base.HandleAsync(context, cancellationToken);
				return;
			}

			this.logger.LogInformation("Handling SOCKS5 UDP ASSOCIATE command.");

			// The client provides a desired address/port, which we must read to advance the stream.
			// In a more advanced implementation, this could be used for access control.
			Memory<byte> headerBuffer = new byte[4 + 1 + 2]; // Header up to port
			await context.ClientChannel.ReadExactlyAsync(headerBuffer, cancellationToken);

			try
			{
				IPEndPoint clientEndpoint = context.ClientChannel.RemoteEndPoint;

				// 1. Create a new UDP relay session.
				await using var udpContext = await udpRelayService.CreateUdpRelaySessionAsync(clientEndpoint, cancellationToken);
				// 2. Send a success reply...
				await this.replyWriter.SendReplyAsync(
					context.ClientChannel,
					Socks5Constants.ReplySucceeded,
					udpContext.BoundEndpoint.Address,
					udpContext.BoundEndpoint.Port,
					cancellationToken);

				// 3. Start the relay loop and hold the TCP connection open.
				await udpContext.StartRelayLoopAsync();
			}
			catch (OperationCanceledException)
			{
				// This is the expected way to exit when the client disconnects.
				this.logger.LogInformation("UDP association ended as client connection was closed.");
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "UDP ASSOCIATE command failed unexpectedly.");
				if (context.ClientChannel.IsConnected)
				{
					await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyGeneralFailure, IPAddress.Any, 0, cancellationToken);
				}
			}
		}
	}
