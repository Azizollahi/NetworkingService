// Copyright By Hossein Azizollahi All Right Reserved.

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
		services.AddPersistence(configuration);
		services.AddSocksServiceApplication(configuration);
		services.AddSocksServiceInfrastructure(configuration);
	}

	public void ConfigureHost(ConfigureHostBuilder builder)
	{
	}

	public void Configure(WebApplication app, ILogger logger)
	{
	}

}
