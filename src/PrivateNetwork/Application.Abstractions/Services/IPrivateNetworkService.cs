// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading.Tasks;

namespace AG.RouterService.PrivateNetwork.Application.Abstractions.Services;

public interface IPrivateNetworkService
{
	Task<Domain.PrivateNetwork> CreateNetworkAsync(string name, string cidrRange);
	Task<bool> AddUserToNetworkAsync(string username, string networkId);
	Task<bool> RemoveUserFromNetworkAsync(string username, string networkId);
	Task<bool> IsConnectionAllowedAsync(string sourceUsername, string destinationIp);
}
