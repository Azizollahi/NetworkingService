// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.PrivateNetwork.Application.Abstractions.Repositories;
using AG.RouterService.PrivateNetwork.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.PrivateNetwork.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddPrivateNetworkInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IPrivateNetworkRepository, PrivateNetworkRepository>();
	}
}
