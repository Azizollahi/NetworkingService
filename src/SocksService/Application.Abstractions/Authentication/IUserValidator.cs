// Copyright By Hossein Azizollahi All Right Reserved.

using System.Threading.Tasks;

namespace AG.RouterService.SocksService.Application.Abstractions.Authentication;

public interface IUserValidator
{
	Task<bool> ValidateAsync(string username, string password);
}
