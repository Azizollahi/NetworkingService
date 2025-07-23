// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace AG.RouterService.SocksService.Application.Abstractions.Models;


public class ConnectionContext
{
	public string Name { get; }
	public Socket ClientSocket { get; }
	public bool IsSslEnabled { get; }
	public X509Certificate2? Certificate { get; }
	public TimeSpan IdleTimeout { get; }

	public ConnectionContext(string name, Socket clientSocket, bool isSslEnabled, X509Certificate2? certificate, TimeSpan idleTimeout)
	{
		Name = name;
		ClientSocket = clientSocket;
		IsSslEnabled = isSslEnabled;
		Certificate = certificate;
		IdleTimeout = idleTimeout;
	}
}
