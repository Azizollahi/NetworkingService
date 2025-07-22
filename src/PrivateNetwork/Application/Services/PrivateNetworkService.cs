// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using AG.RouterService.AuthService.Application.Abstractions.Repositories;
using AG.RouterService.PrivateNetwork.Application.Abstractions.Repositories;
using AG.RouterService.PrivateNetwork.Application.Abstractions.Services;

namespace AG.RouterService.PrivateNetwork.Application.Services;

internal sealed class PrivateNetworkService : IPrivateNetworkService
{
	private readonly IPrivateNetworkRepository networkRepository;
	private readonly IUserRepository userRepository;

	public PrivateNetworkService(IPrivateNetworkRepository networkRepository, IUserRepository userRepository)
	{
		this.networkRepository = networkRepository;
		this.userRepository = userRepository;
	}

	public async Task<Domain.PrivateNetwork> CreateNetworkAsync(string name, string cidrRange)
	{
		var network = new Domain.PrivateNetwork
		{
			Name = name,
			IpRange = cidrRange
		};
		await networkRepository.AddAsync(network);
		return network;
	}

	public async Task<bool> AddUserToNetworkAsync(string username, string networkId)
	{
		var user = await userRepository.GetByUsernameAsync(username);
		if (user is null) return false; // User does not exist

		var network = await networkRepository.GetByIdAsync(networkId);
		if (network is null) return false; // Network does not exist

		var newMember = network.AddMember(user.Username);
		if (newMember is null) return false; // User already in network or network is full

		await networkRepository.UpdateAsync(network);
		return true;
	}

	public async Task<bool> RemoveUserFromNetworkAsync(string username, string networkId)
	{
		var network = await networkRepository.GetByIdAsync(networkId);
		if (network is null) return false;

		network.RemoveMember(username);
		await networkRepository.UpdateAsync(network);
		return true;
	}
	public async Task<bool> IsConnectionAllowedAsync(string sourceUsername, string destinationIp)
	{
		var allNetworks = await this.networkRepository.GetAllAsync();

		// Find all networks the source user is a member of
		var userNetworks = allNetworks.Where(n =>
			n.Members.Any(m => m.Username.Equals(sourceUsername, StringComparison.OrdinalIgnoreCase)));

		foreach (var network in userNetworks)
		{
			// Check if the destination IP belongs to another member in the SAME network
			if (network.Members.Any(m => m.AssignedIp == destinationIp))
			{
				// The destination is a valid member of a network the source user belongs to.
				return true;
			}
		}

		// If we get here, no shared network was found that contains the destination IP.
		return false;
	}
}
