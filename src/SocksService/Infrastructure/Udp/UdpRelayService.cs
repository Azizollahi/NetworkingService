// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Udp;

internal sealed class UdpRelayService : IUdpRelayService
{
	private readonly IAccessControlService accessControlService;
	private readonly ILogger<UdpRelaySession> logger; // Note: Logger for the session class

	public UdpRelayService(IAccessControlService accessControlService, ILogger<UdpRelaySession> logger)
	{
		this.accessControlService = accessControlService;
		this.logger = logger;
	}

	public Task<UdpRelayContext> CreateUdpRelaySessionAsync(IPEndPoint clientEndpoint, CancellationToken lifetimeToken)
	{
		// The service now acts as a factory for sessions
		var session = new UdpRelaySession(clientEndpoint, lifetimeToken, this.accessControlService, this.logger);
		return Task.FromResult<UdpRelayContext>(session);
	}
}
