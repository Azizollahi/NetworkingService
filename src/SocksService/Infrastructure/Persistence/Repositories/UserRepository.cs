// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Domain;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Persistence.Repositories;

public sealed class PersistenceOptions
{
	public string FilePath { get; set; } = "./";
}

internal sealed class UserRepository : IUserValidator
{
	private readonly string userFilePath;
	private List<SocksUser> users = new();

	public UserRepository(IOptions<PersistenceOptions> options)
	{
		this.userFilePath = Path.Combine(options.Value.FilePath, "users.json");
		LoadUsersFromFile();
	}

	private void LoadUsersFromFile()
	{
		if (!File.Exists(this.userFilePath))
		{
			this.users = new List<SocksUser>();
			return;
		}

		string json = File.ReadAllText(this.userFilePath);
		this.users = JsonSerializer.Deserialize<List<SocksUser>>(json) ?? new List<SocksUser>();
	}

	public Task<bool> ValidateAsync(string username, string password)
	{
		SocksUser? user = this.users.FirstOrDefault(u => u.Username == username);
		if (user is null)
		{
			// User not found
			return Task.FromResult(false);
		}

		bool isValid = false;
		try
		{
			// BCrypt will securely compare the provided password with the stored hash.
			isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
		}
		catch (BCrypt.Net.SaltParseException)
		{
			// This can happen if the stored password is not a valid BCrypt hash.
			// Log this error in a real application.
			isValid = false;
		}

		return Task.FromResult(isValid);
	}
}
