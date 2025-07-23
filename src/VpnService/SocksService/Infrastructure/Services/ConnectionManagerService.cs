// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Concurrent;
using System.Threading;
using AG.RouterService.SocksService.Application.Abstractions.Services;

namespace AG.RouterService.SocksService.Infrastructure.Services;

public class ConnectionManagerService : IConnectionManagerService
{
	private readonly ConcurrentDictionary<string, SemaphoreSlim> listenerSemaphores = new();

	public bool TryAcceptConnection(string listenerName, int maxConnections)
	{
		var semaphore = this.listenerSemaphores.GetOrAdd(listenerName, new SemaphoreSlim(maxConnections, maxConnections));
		return semaphore.Wait(0); // Returns false immediately if the semaphore is full
	}

	public void ReleaseConnection(string listenerName)
	{
		if (this.listenerSemaphores.TryGetValue(listenerName, out var semaphore))
		{
			semaphore.Release();
		}
	}
}
