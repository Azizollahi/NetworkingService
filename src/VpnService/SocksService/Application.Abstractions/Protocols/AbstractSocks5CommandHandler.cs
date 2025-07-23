// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Protocols;

public abstract class AbstractSocks5CommandHandler : ISocks5CommandHandler
{
	private ISocks5CommandHandler? nextHandler;

	public ISocks5CommandHandler SetNext(ISocks5CommandHandler handler)
	{
		this.nextHandler = handler;
		return handler;
	}

	public virtual async Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken)
	{
		if (this.nextHandler is not null)
		{
			await this.nextHandler.HandleAsync(context, cancellationToken);
		}
	}
}

public interface ISocks5CommandHandler
{
	ISocks5CommandHandler SetNext(ISocks5CommandHandler handler);
	Task HandleAsync(Socks5CommandContext context, CancellationToken cancellationToken);
}
