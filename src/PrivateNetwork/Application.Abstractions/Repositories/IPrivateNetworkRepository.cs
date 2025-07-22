// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AG.RouterService.PrivateNetwork.Application.Abstractions.Repositories;

public interface IPrivateNetworkRepository
{
	Task<Domain.PrivateNetwork?> GetByIdAsync(string id);
	Task<IEnumerable<Domain.PrivateNetwork>> GetAllAsync();
	Task AddAsync(Domain.PrivateNetwork network);
	Task UpdateAsync(Domain.PrivateNetwork network);
	Task DeleteAsync(string id);
}
