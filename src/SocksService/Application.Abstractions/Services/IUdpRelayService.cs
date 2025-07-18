// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IUdpRelayService
{
	Task<UdpRelayContext> StartUdpRelaySessionAsync(CancellationToken lifetimeToken);
}
