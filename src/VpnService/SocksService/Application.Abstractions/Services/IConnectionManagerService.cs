// Copyright By Hossein Azizollahi All Right Reserved.

namespace AG.RouterService.SocksService.Application.Abstractions.Services;

public interface IConnectionManagerService
{
	bool TryAcceptConnection(string listenerName, int maxConnections);
	void ReleaseConnection(string listenerName);
}
