// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.AuthService.Infrastructure.Extensions;
using AG.RouterService.Infrastructure.Persistence.Extensions;
using AG.RouterService.SocksService.Application.Extensions;
using AG.RouterService.SocksService.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AG.RouterService;

public class Startup
{

	private readonly IConfiguration configuration;
	private readonly IWebHostEnvironment env;

	public Startup(IConfiguration configuration, IWebHostEnvironment env)
	{
		this.configuration = configuration;
		this.env = env;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		// Shared
		services.AddPersistence(configuration);

		// SocksService Module
		services.AddSocksServiceApplication(configuration);
		services.AddSocksServiceInfrastructure(configuration);

		// AuthService Module
		services.AddAuthServiceInfrastructure(configuration);

		// PrivateNetwork Module

	}

	public void ConfigureHost(ConfigureHostBuilder builder)
	{
	}

	public void Configure(WebApplication app, ILogger logger)
	{
	}

}
