// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.AuthService.Application.Abstractions.Repositories;
using AG.RouterService.AuthService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.AuthService.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddAuthServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<PersistenceOptions>()
			.Bind(configuration.GetSection("Persistence"));

		services.AddSingleton<IUserRepository, UserRepository>();
	}
}
