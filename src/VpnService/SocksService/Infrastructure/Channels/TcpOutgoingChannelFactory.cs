// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class TcpOutgoingChannelFactory : IOutgoingChannelFactory
{
	private readonly ILogger<TcpOutgoingChannelFactory> logger;

	public TcpOutgoingChannelFactory(ILogger<TcpOutgoingChannelFactory> logger)
	{
		this.logger = logger;
	}

	public async Task<IChannel?> CreateConnectionAsync(string host, int port, CancellationToken cancellationToken)
	{
		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			await socket.ConnectAsync(host, port, cancellationToken);
			this.logger.LogInformation("Successfully connected to target {Host}:{Port}", host, port);
			return new TcpChannel(socket);
		}
		catch (SocketException ex)
		{
			this.logger.LogError(ex, "Failed to connect to target {Host}:{Port}", host, port);
			return null;
		}
	}
}
