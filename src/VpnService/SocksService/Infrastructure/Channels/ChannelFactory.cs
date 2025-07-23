// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net.Sockets;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class ChannelFactory : IChannelFactory
{
	public IChannel Create(Socket socket)
	{
		return new TcpChannel(socket);
	}
}
