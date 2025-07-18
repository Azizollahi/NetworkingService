// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Services;
using AG.RouterService.SocksService.Domain;
using AG.RouterService.SocksService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Services;

internal sealed class AccessControlService : IAccessControlService
{
	private readonly List<AccessRule> sourceRules;
	private readonly List<AccessRule> destinationRules;

	public AccessControlService(IOptions<PersistenceOptions> options)
	{
		var rulesFilePath = Path.Combine(options.Value.FilePath, "rules.json");
		var allRules = LoadRulesFromFile(rulesFilePath);

		// Pre-sort rules for efficient processing. Deny rules are checked first.
		this.sourceRules = allRules.Where(r => r.Target == TargetType.SourceIp).OrderBy(r => r.Type).ToList();
		this.destinationRules =
			allRules.Where(r => r.Target == TargetType.DestinationHost).OrderBy(r => r.Type).ToList();
	}

	public Task<bool> IsSourceAllowedAsync(IPAddress sourceIpAddress)
	{
		// Find the first matching rule. Since Deny rules come first, they take precedence.
		var rule = this.sourceRules.FirstOrDefault(r => Matches(sourceIpAddress.ToString(), r.Pattern));
		bool isAllowed =
			rule?.Type == RuleType.Allow; // Allowed only if an Allow rule matches. Deny or no match = deny.
		return Task.FromResult(isAllowed);
	}

	public Task<bool> IsDestinationAllowedAsync(string destinationHost)
	{
		var rule = this.destinationRules.FirstOrDefault(r => Matches(destinationHost, r.Pattern));
		bool isAllowed = rule?.Type == RuleType.Allow;
		return Task.FromResult(isAllowed);
	}

	private bool Matches(string input, string pattern)
	{
		if (pattern == "*") return true;
		if (pattern.StartsWith("*") && pattern.EndsWith("*"))
			return input.Contains(pattern.Trim('*'));
		if (pattern.StartsWith("*"))
			return input.EndsWith(pattern.Substring(1));
		if (pattern.EndsWith("*"))
			return input.StartsWith(pattern.Substring(0, pattern.Length - 1));

		return input.Equals(pattern, System.StringComparison.OrdinalIgnoreCase);
	}

	private List<AccessRule> LoadRulesFromFile(string filePath)
	{
		if (!File.Exists(filePath)) return new List<AccessRule>();
		string json = File.ReadAllText(filePath);
		return JsonSerializer.Deserialize<List<AccessRule>>(json) ?? new List<AccessRule>();
	}
}
