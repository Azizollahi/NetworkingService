// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Threading.Tasks;
using AG.RouterService.AuthService.Application.Abstractions.Repositories;
using AG.RouterService.PrivateNetwork.Application.Abstractions.Repositories;

namespace AG.RouterService.PrivateNetwork.Application.Services;

public interface IPrivateNetworkService
{
	Task<Domain.PrivateNetwork> CreateNetworkAsync(string name, string cidrRange);
	Task<bool> AddUserToNetworkAsync(string username, string networkId);
	Task<bool> RemoveUserFromNetworkAsync(string username, string networkId);
}

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
}
