// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal sealed class Socks5ReplyWriter : ISocks5ReplyWriter
{
	public Task SendReplyAsync(
		IChannel channel,
		byte replyCode,
		IPAddress boundAddress,
		int boundPort,
		CancellationToken cancellationToken)
	{
		byte[] addressBytes = boundAddress.GetAddressBytes();
		byte addressType = (boundAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
			? Socks5Constants.AddressTypeIPv6
			: Socks5Constants.AddressTypeIPv4;

		byte[] portBytes = { (byte)(boundPort >> 8), (byte)boundPort };

		byte[] reply = new byte[4 + addressBytes.Length + 2];
		reply[0] = Socks5Constants.Version;
		reply[1] = replyCode;
		reply[2] = 0x00; // RSV
		reply[3] = addressType;
		addressBytes.CopyTo(reply, 4);
		portBytes.CopyTo(reply, 4 + addressBytes.Length);

		return channel.WriteAsync(reply, cancellationToken).AsTask();
	}
}
