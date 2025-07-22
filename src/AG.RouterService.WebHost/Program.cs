// See https://aka.ms/new-console-template for more information

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AG.RouterService;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		int endYear = DateTime.Now.Year < 2025 ? 2025 : DateTime.Now.Year;
		Console.WriteLine($"Copyright © 2025-{endYear} Hossein Azizollahi. All rights reserved.");
		Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
		builder.Configuration.Sources.Clear();

		LoadConfigs(builder);

		builder.Host.UseSerilog((context, services, configuration) => configuration
			.ReadFrom.Configuration(context.Configuration)
			.ReadFrom.Services(services)
			.Enrich.FromLogContext());

		builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer(); // Required for Swagger
		builder.Services.AddSwaggerGen(); // Add Swagger for Controllers

		builder.Services.AddMediatR(opt =>
		{
			opt.RegisterServicesFromAssembly(typeof(AG.RouterService.SocksService.Application.AssemblyPointer).Assembly);
		});
		var startup = new Startup(builder.Configuration, builder.Environment);
		startup.ConfigureHost(builder.Host);
		startup.ConfigureServices(builder.Services);

		builder.Services.AddCors(s=> s.AddPolicy("AllowAll", policy =>
			policy
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader()));

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.MapControllers();

		var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
		startup.Configure(app, loggerFactory.CreateLogger("Startup"));

		app.UseCors("AllowAll");
		app.UseAuthorization();

		app.Run();
	}
	private static void LoadConfigs(WebApplicationBuilder builder)
	{
		builder.Configuration.AddEnvironmentVariables();
		if (builder.Environment.IsProduction())
		{
			builder.Configuration.AddJsonFile($"./appsettings.json");
		}
		else if (builder.Environment.IsStaging())
		{
			builder.Configuration.AddJsonFile($"./appsettings.json");
		}
		else
		{
			builder.Configuration.AddJsonFile($"./appsettings.{builder.Environment.EnvironmentName}.json", false, true);
		}
	}
}