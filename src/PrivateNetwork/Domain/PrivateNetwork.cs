// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace AG.RouterService.PrivateNetwork.Domain;

public class PrivateNetwork
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = string.Empty;
	public string IpRange { get; set; } = string.Empty; // e.g., "10.1.0.0/16"
	public List<NetworkMember> Members { get; set; } = new();

	public NetworkMember? AddMember(string username)
	{
		if (this.Members.Any(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
		{
			return null; // User is already a member
		}

		if (!IPNetwork.TryParse(this.IpRange, out var network))
		{
			throw new InvalidOperationException($"Invalid IP range format: {this.IpRange}");
		}

		if (network.BaseAddress.AddressFamily != AddressFamily.InterNetwork)
		{
			throw new NotSupportedException("Only IPv4 ranges are supported for automatic IP assignment.");
		}

		var assignedIps = new HashSet<IPAddress>(this.Members.Select(m => IPAddress.Parse(m.AssignedIp)));

		// Correctly calculate the network boundaries
		uint baseAddress = AddressAsUInt(network.BaseAddress);
		uint mask = ~(uint.MaxValue >> network.PrefixLength);
		uint broadcastAddress = baseAddress | ~mask;

		uint firstIp = baseAddress + 1;
		uint lastIp = broadcastAddress - 1;

		for (uint currentIpInt = firstIp; currentIpInt <= lastIp; currentIpInt++)
		{
			var currentIp = UIntAsAddress(currentIpInt);
			if (!assignedIps.Contains(currentIp))
			{
				var newMember = new NetworkMember
				{
					Username = username,
					AssignedIp = currentIp.ToString()
				};
				this.Members.Add(newMember);
				return newMember;
			}
		}

		return null; // No available IP addresses
	}

	public void RemoveMember(string username)
	{
		this.Members.RemoveAll(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
	}

	// --- Helper Methods for IP Address Calculation ---

	private static uint AddressAsUInt(IPAddress address)
	{
		byte[] bytes = address.GetAddressBytes();
		// Convert big-endian byte order to a little-endian uint
		return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
	}

	private static IPAddress UIntAsAddress(uint address)
	{
		// Convert little-endian uint back to a big-endian byte array
		byte[] bytes = new byte[4];
		bytes[0] = (byte)((address >> 24) & 0xFF);
		bytes[1] = (byte)((address >> 16) & 0xFF);
		bytes[2] = (byte)((address >> 8) & 0xFF);
		bytes[3] = (byte)(address & 0xFF);
		return new IPAddress(bytes);
	}
}
