// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Protocols;

public class Socks5CommandContext
{
	public IChannel ClientChannel { get; }
	public ReadOnlyMemory<byte> CommandHeader { get; }
	public byte Command => CommandHeader.Span[1];
	public TimeSpan IdleTimeout { get; }
	public string? AuthenticatedUsername { get; }

	public Socks5CommandContext(IChannel clientChannel, ReadOnlyMemory<byte> commandHeader, TimeSpan idleTimeout, string? username)
	{
		ClientChannel = clientChannel;
		CommandHeader = commandHeader;
		IdleTimeout = idleTimeout;
		AuthenticatedUsername = username;
	}
}
