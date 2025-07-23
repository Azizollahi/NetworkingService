// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net.Sockets;
using AG.RouterService.SocksService.Application.Abstractions.Services;

namespace AG.RouterService.SocksService.Application.Abstractions.Channels;

public interface IChannelFactory
{
	IChannel Create(Socket socket);
}
