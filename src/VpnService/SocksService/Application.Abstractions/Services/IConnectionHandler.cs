// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Models;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IConnectionHandler
{
	Task HandleConnectionAsync(ConnectionContext context, CancellationToken cancellationToken);
}
