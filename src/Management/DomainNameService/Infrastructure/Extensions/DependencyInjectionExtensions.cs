// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.DomainNameService.Application.Abstractions.Services;
using AG.RouterService.DomainNameService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.DomainNameService.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
	public static void AddDomainNameServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDnsResolverService, FileBasedDnsResolver>();
	}
}
