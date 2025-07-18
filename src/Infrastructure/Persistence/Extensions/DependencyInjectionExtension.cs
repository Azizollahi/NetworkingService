// Copyright By Hossein Azizollahi All Right Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AG.RouterService.Infrastructure.Persistence.Extensions;

public static class DependencyInjectionExtension
{
	public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		// Register your persistence-related services here
		// For example, DbContext, Repositories, etc.
		// services.AddDbContext<YourDbContext>(options => ...);
		// services.AddScoped<IYourRepository, YourRepository>();

		// Example:
		// services.AddScoped<IRecipeRepository, RecipeRepository>();

		// Note: Ensure to add necessary using directives for your DbContext and repositories.
	}
}
