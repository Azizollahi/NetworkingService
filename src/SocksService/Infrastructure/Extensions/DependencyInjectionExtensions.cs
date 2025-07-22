// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Factories;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Infrastructure.Authentication;
using AG.RouterService.SocksService.Infrastructure.Channels;
using AG.RouterService.SocksService.Infrastructure.Factories;
using AG.RouterService.SocksService.Infrastructure.Listeners;
using AG.RouterService.SocksService.Infrastructure.Persistence.Repositories;
using AG.RouterService.SocksService.Infrastructure.Protocols;
using AG.RouterService.SocksService.Infrastructure.Protocols.CommandHandlers;
using AG.RouterService.SocksService.Infrastructure.Services;
using AG.RouterService.SocksService.Infrastructure.Udp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AG.RouterService.SocksService.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddSocksServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<List<ListenerOptions>>().Bind(configuration.GetSection("Listeners"));
		services.AddHostedService<ListenerManagerService>();

		services.AddSingleton<ConnectionPairingService>();
		services.AddSingleton<IConnectionPairingService>(provider =>
			provider.GetRequiredService<ConnectionPairingService>());
		services.AddHostedService(provider =>
			provider.GetRequiredService<ConnectionPairingService>());

		services.AddSingleton<IChannelFactory, ChannelFactory>();
		services.AddSingleton<IOutgoingChannelFactory, TcpOutgoingChannelFactory>();
		services.AddSingleton<ISecureChannelFactory, SslChannelFactory>();

		services.AddSingleton<IProtocolHandler, Socks5ProtocolHandler>();

		services.AddSingleton<IProtocolHandler, Socks4ProtocolHandler>();
		services.AddSingleton<IConnectionManagerService, ConnectionManagerService>();

		services.AddSingleton<ISocks5Authenticator, NoAuthenticationAuthenticator>();
		services.AddSingleton<ISocks5Authenticator, UsernamePasswordAuthenticator>();
		services.AddSingleton<ISocks5Authenticator, GssapiAuthenticator>();

		services.AddSingleton<ISocks5AddressReader, Socks5AddressReader>();
		services.AddSingleton<ISocks5ReplyWriter, Socks5ReplyWriter>();

		services.AddOptions<PersistenceOptions>()
			.Bind(configuration.GetSection("Persistence"));

		services.AddSingleton<IUserValidator, UserRepository>();

		services.AddSingleton<IUdpRelayService, UdpRelayService>();

		services.AddSingleton<IAccessControlService, AccessControlService>();

		services.AddSingleton<ConnectCommandHandler>();
		services.AddSingleton<BindCommandHandler>();
		services.AddSingleton<UdpAssociateCommandHandler>();
		services.AddSingleton<UnsupportedCommandHandler>();
		services.AddSingleton<ISocks5CommandHandler>(provider =>
		{
			// Resolve all handlers
			var connectHandler = provider.GetRequiredService<ConnectCommandHandler>();
			var bindHandler = provider.GetRequiredService<BindCommandHandler>();
			var udpAssociateHandler = provider.GetRequiredService<UdpAssociateCommandHandler>();
			var unsupportedHandler = provider.GetRequiredService<UnsupportedCommandHandler>();

			// Link them together
			connectHandler.SetNext(bindHandler);
			bindHandler.SetNext(udpAssociateHandler);
			udpAssociateHandler.SetNext(unsupportedHandler);

			// Return the first handler in the chain
			return connectHandler;
		});


	}

}
