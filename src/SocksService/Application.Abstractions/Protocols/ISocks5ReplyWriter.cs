// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Protocols;

public interface ISocks5ReplyWriter
{
	Task SendReplyAsync(
		IChannel channel,
		byte replyCode,
		IPAddress boundAddress,
		int boundPort,
		CancellationToken cancellationToken);
}
