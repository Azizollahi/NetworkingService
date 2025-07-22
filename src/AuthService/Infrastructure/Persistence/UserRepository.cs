// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.AuthService.Application.Abstractions.Repositories;
using AG.RouterService.AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AG.RouterService.AuthService.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
	public string FilePath { get; set; } = "./";
}

internal sealed class UserRepository : IUserRepository
{
	private readonly string userFilePath;
	private List<User> users = new();
	private readonly SemaphoreSlim fileLock = new(1, 1);

	public UserRepository(IOptions<PersistenceOptions> options)
	{
		this.userFilePath = Path.Combine(options.Value.FilePath, "users.json");
		LoadUsersFromFile().GetAwaiter().GetResult();
	}

	public Task<User?> GetByUsernameAsync(string username)
	{
		return Task.FromResult(this.users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase)));
	}

	public Task<IEnumerable<User>> GetAllAsync()
	{
		return Task.FromResult<IEnumerable<User>>(this.users);
	}

	public async Task AddAsync(User user)
	{
		await this.fileLock.WaitAsync();
		try
		{
			this.users.Add(user);
			await PersistUsersToFile();
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	public async Task UpdateAsync(User user)
	{
		await this.fileLock.WaitAsync();
		try
		{
			var existingUser = this.users.FirstOrDefault(u => u.Username.Equals(user.Username, System.StringComparison.OrdinalIgnoreCase));
			if(existingUser is not null)
			{
				existingUser.Password = user.Password;
				await PersistUsersToFile();
			}
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	public async Task DeleteAsync(string username)
	{
		await this.fileLock.WaitAsync();
		try
		{
			var userToRemove = this.users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
			if(userToRemove is not null)
			{
				this.users.Remove(userToRemove);
				await PersistUsersToFile();
			}
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	public Task<bool> ValidateAsync(string username, string password)
	{
		var user = this.users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
		if (user is null) return Task.FromResult(false);

		try
		{
			return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.Password));
		}
		catch (BCrypt.Net.SaltParseException)
		{
			return Task.FromResult(false);
		}
	}

	private async Task LoadUsersFromFile()
	{
		if (!File.Exists(this.userFilePath))
		{
			this.users = new List<User>();
			return;
		}

		string json = await File.ReadAllTextAsync(this.userFilePath);
		this.users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
	}

	private async Task PersistUsersToFile()
	{
		string json = JsonSerializer.Serialize(this.users, new JsonSerializerOptions { WriteIndented = true });
		await File.WriteAllTextAsync(this.userFilePath, json);
	}
}
