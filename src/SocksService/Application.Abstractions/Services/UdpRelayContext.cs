// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public class UdpRelayContext
{
	public IPEndPoint BoundEndpoint { get; }

	public UdpRelayContext(IPEndPoint boundEndpoint)
	{
		this.BoundEndpoint = boundEndpoint;
	}
}
