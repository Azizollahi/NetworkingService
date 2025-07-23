// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AG.RouterService.DomainNameService.Application.Abstractions.Services;

public interface IDnsResolverService
{
	/// <summary>
	/// Resolves a domain name against the private hosts list.
	/// </summary>
	/// <returns>The resolved IPAddress, or null if no record was found.</returns>
	Task<List<IPAddress>> ResolveAsync(string domainName);
}
