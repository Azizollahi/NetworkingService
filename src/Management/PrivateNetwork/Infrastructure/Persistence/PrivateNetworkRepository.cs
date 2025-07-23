// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.PrivateNetwork.Application.Abstractions.Repositories;
using Microsoft.Extensions.Options;

namespace AG.RouterService.PrivateNetwork.Infrastructure.Persistence;

internal sealed class PrivateNetworkRepository : IPrivateNetworkRepository
{
	private readonly string networkFilePath;
	private List<Domain.PrivateNetwork> networks = new();
	private readonly SemaphoreSlim fileLock = new(1, 1);

	public PrivateNetworkRepository(IOptions<PersistenceOptions> options)
	{
		this.networkFilePath = Path.Combine(options.Value.FilePath, "networks.json");
		LoadNetworksFromFile().GetAwaiter().GetResult();
	}

	public Task<Domain.PrivateNetwork?> GetByIdAsync(string id)
	{
		return Task.FromResult(this.networks.FirstOrDefault(n => n.Id == id));
	}

	public Task<IEnumerable<Domain.PrivateNetwork>> GetAllAsync()
	{
		return Task.FromResult<IEnumerable<Domain.PrivateNetwork>>(this.networks);
	}

	public async Task AddAsync(Domain.PrivateNetwork network)
	{
		await this.fileLock.WaitAsync();
		try
		{
			this.networks.Add(network);
			await PersistNetworksToFile();
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	public async Task UpdateAsync(Domain.PrivateNetwork network)
	{
		await this.fileLock.WaitAsync();
		try
		{
			var index = this.networks.FindIndex(n => n.Id == network.Id);
			if (index != -1)
			{
				this.networks[index] = network;
				await PersistNetworksToFile();
			}
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	public async Task DeleteAsync(string id)
	{
		await this.fileLock.WaitAsync();
		try
		{
			this.networks.RemoveAll(n => n.Id == id);
			await PersistNetworksToFile();
		}
		finally
		{
			this.fileLock.Release();
		}
	}

	private async Task LoadNetworksFromFile()
	{
		if (!File.Exists(this.networkFilePath))
		{
			this.networks = new List<Domain.PrivateNetwork>();
			return;
		}
		var json = await File.ReadAllTextAsync(this.networkFilePath);
		this.networks = JsonSerializer.Deserialize<List<Domain.PrivateNetwork>>(json) ?? new List<Domain.PrivateNetwork>();
	}

	private async Task PersistNetworksToFile()
	{
		var json = JsonSerializer.Serialize(this.networks, new JsonSerializerOptions { WriteIndented = true });
		await File.WriteAllTextAsync(this.networkFilePath, json);
	}
}
