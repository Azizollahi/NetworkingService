// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Exceptions;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols.CommandHandlers;

internal sealed class BindCommandHandler : AbstractSocks5CommandHandler
	{
		private readonly ILogger<BindCommandHandler> logger;
		private readonly IDataRelayService dataRelayService;
		private readonly IChannelFactory channelFactory;
		private readonly ISocks5AddressReader addressReader;
		private readonly ISocks5ReplyWriter replyWriter;

		public BindCommandHandler(
			ILogger<BindCommandHandler> logger,
			IDataRelayService dataRelayService,
			IChannelFactory channelFactory,
			ISocks5AddressReader addressReader,
			ISocks5ReplyWriter replyWriter)
		{
			this.logger = logger;
			this.dataRelayService = dataRelayService;
			this.channelFactory = channelFactory;
			this.addressReader = addressReader;
			this.replyWriter = replyWriter;
		}

		public override async Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken)
		{
			if (context.Command != Socks5Constants.CommandBind)
			{
				await base.HandleAsync(context, cancellationToken);
				return;
			}

			this.logger.LogInformation("Handling SOCKS5 BIND command.");

			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IChannel? targetChannel = null;

			try
			{
				// The client sends the address of the application server it expects a connection from.
				// We must read this to advance the stream, even if we don't use it for validation here.
				(string? expectedHost, int expectedPort) = await this.addressReader.ReadAddressAndPortAsync(context.ClientChannel, context.CommandHeader, cancellationToken);
				if (expectedHost is null) return;

				// 1. Bind to a local port and start listening.
				listener.Bind(new IPEndPoint(IPAddress.Any, 0)); // Port 0 lets the OS pick an available port
				listener.Listen(1); // We only expect one incoming connection
				IPEndPoint listeningEndpoint = (IPEndPoint)listener.LocalEndPoint!;
				this.logger.LogInformation("BIND: Listening for target application server on {Endpoint}", listeningEndpoint);

				// 2. Send the FIRST reply to the client with the IP and port of our new listener.
				await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplySucceeded, listeningEndpoint.Address, listeningEndpoint.Port, cancellationToken);

				// 3. Wait for the application server to connect to our listener.
				Socket targetSocket = await listener.AcceptAsync(cancellationToken);
				this.logger.LogInformation("BIND: Accepted connection from {RemoteEndpoint}", targetSocket.RemoteEndPoint);

				// Optional: Here you could add a security check to ensure targetSocket.RemoteEndPoint.Address
				// matches the IP resolved from expectedHost.

				// 4. Send the SECOND reply to the client, confirming the connection was successful.
				// The reply includes the endpoint of the connecting host.
				IPEndPoint targetEndpoint = (IPEndPoint)targetSocket.RemoteEndPoint!;
				await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplySucceeded, targetEndpoint.Address, targetEndpoint.Port, cancellationToken);

				// 5. Create a channel for the new connection and start relaying data.
				targetChannel = this.channelFactory.Create(targetSocket);
				await this.dataRelayService.RelayAsync(context.ClientChannel, targetChannel, cancellationToken);
			}
			catch (UnsupportedAddressTypeException ex)
			{
				this.logger.LogWarning(ex.Message);
				await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyAddressTypeNotSupported, IPAddress.Any, 0, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				this.logger.LogError(ex, "BIND command failed unexpectedly.");
				if (context.ClientChannel.IsConnected)
				{
					await this.replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyGeneralFailure, IPAddress.Any, 0, cancellationToken);
				}
			}
			finally
			{
				// Ensure all created resources are closed.
				listener.Close();
				if (targetChannel is not null)
				{
					await targetChannel.CloseAsync();
				}
			}
		}
	}
