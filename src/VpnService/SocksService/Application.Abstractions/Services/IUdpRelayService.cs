// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IUdpRelayService
{
	Task<UdpRelayContext> CreateUdpRelaySessionAsync(IPEndPoint clientEndpoint, CancellationToken lifetimeToken);
}
