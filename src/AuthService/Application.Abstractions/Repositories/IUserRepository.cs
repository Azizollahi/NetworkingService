// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using AG.RouterService.AuthService.Domain;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;

namespace AG.RouterService.AuthService.Application.Abstractions.Repositories;

public interface IUserRepository : IUserValidator
{
	Task<User?> GetByUsernameAsync(string username);
	Task<IEnumerable<User>> GetAllAsync();
	Task AddAsync(User user);
	Task UpdateAsync(User user);
	Task DeleteAsync(string username);
}
