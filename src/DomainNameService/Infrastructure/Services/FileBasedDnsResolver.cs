// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AG.RouterService.DomainNameService.Application.Abstractions.Services;
using AG.RouterService.DomainNameService.Domain;
using AG.RouterService.DomainNameService.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AG.RouterService.DomainNameService.Infrastructure.Services;

internal sealed class FileBasedDnsResolver : IDnsResolverService
{
	private readonly Dictionary<string, List<IPAddress>> records;

	public FileBasedDnsResolver(IOptions<PersistenceOptions> options)
	{
		var hostsFilePath = Path.Combine(options.Value.FilePath, "hosts.json");
		this.records = LoadRecordsFromFile(hostsFilePath);
	}

	public Task<List<IPAddress>> ResolveAsync(string domainName)
	{
		if (this.records.TryGetValue(domainName, out var ipAddress))
		{
			return Task.FromResult<List<IPAddress>>(ipAddress);
		}
		return Task.FromResult<List<IPAddress>>(null);
	}

	private static Dictionary<string, List<IPAddress>> LoadRecordsFromFile(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new Dictionary<string, List<IPAddress>>(System.StringComparer.OrdinalIgnoreCase);
		}

		string json = File.ReadAllText(filePath);
		var recordsList = JsonSerializer.Deserialize<List<HostRecord>>(json) ?? new List<HostRecord>();

		return new Dictionary<string, List<IPAddress>>(
			recordsList.ToDictionary(r => r.Hostname, r => r.IpAddress.Select(IPAddress.Parse).ToList()),
			System.StringComparer.OrdinalIgnoreCase);
	}
}
