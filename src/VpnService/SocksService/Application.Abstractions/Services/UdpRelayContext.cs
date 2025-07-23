// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public abstract class UdpRelayContext : IAsyncDisposable
{
	public abstract IPEndPoint BoundEndpoint { get; }
	public abstract Task StartRelayLoopAsync();
	public abstract ValueTask DisposeAsync();
}
