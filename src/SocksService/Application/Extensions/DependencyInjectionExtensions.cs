// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.SocksService.Application.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddSocksServiceApplication(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IConnectionHandler, ConnectionHandlerService>();
		services.AddSingleton<IProtocolDispatcher, ProtocolDispatcher>();
		services.AddSingleton<IDataRelayService, DataRelayService>();
	}
}
