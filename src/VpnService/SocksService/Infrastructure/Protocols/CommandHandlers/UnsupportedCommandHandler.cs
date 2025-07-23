// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols.CommandHandlers;

internal sealed class UnsupportedCommandHandler : AbstractSocks5CommandHandler
{
	private readonly ILogger<UnsupportedCommandHandler> logger;
	private readonly ISocks5ReplyWriter replyWriter;

	public UnsupportedCommandHandler(ILogger<UnsupportedCommandHandler> logger,
		ISocks5ReplyWriter replyWriter)
	{
		this.logger = logger;
		this.replyWriter = replyWriter;
	}

	public override async Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken)
	{
		logger.LogWarning("SOCKS5 command {Command} is not supported.", context.Command);
		await replyWriter.SendReplyAsync(context.ClientChannel, Socks5Constants.ReplyCommandNotSupported, IPAddress.Any, 0, cancellationToken);
	}
}
