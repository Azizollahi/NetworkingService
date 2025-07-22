// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.PrivateNetwork.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.PrivateNetwork.Application.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddPrivateNetworkApplication(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IPrivateNetworkService, PrivateNetworkService>();
	}
}
