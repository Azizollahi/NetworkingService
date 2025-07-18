// Copyright By Hossein Azizollahi All Right Reserved.

using System.Net;
using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IAccessControlService
{
	Task<bool> IsSourceAllowedAsync(IPAddress sourceIpAddress);
	Task<bool> IsDestinationAllowedAsync(string destinationHost);
}
