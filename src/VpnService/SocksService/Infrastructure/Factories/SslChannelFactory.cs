// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Factories;
using AG.RouterService.SocksService.Infrastructure.Channels;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Factories;

internal sealed class SslChannelFactory : ISecureChannelFactory
{
	private readonly ILogger<SslChannelFactory> logger;

	public SslChannelFactory(ILogger<SslChannelFactory> logger)
	{
		this.logger = logger;
	}

	public async Task<IChannel> CreateSecureChannelAsync(string name, IChannel underlyingChannel, X509Certificate2 certificate, CancellationToken cancellationToken)
	{
		this.logger.LogInformation("Performing SSL/TLS handshake... Name: {Name}, Certificate: {Certificate}", name, certificate.Subject);

		var channelStream = new ChannelStream(underlyingChannel);
		var sslStream = new SslStream(channelStream, leaveInnerStreamOpen: false);

		try
		{
			await sslStream.AuthenticateAsServerAsync(certificate, clientCertificateRequired: false, checkCertificateRevocation: true);
			this.logger.LogInformation("SSL handshake successful. Listener: {Name}, Cipher: {CipherAlgorithm}, Protocol: {SslProtocol}", name, sslStream.CipherAlgorithm, sslStream.SslProtocol);

			var remoteEndPoint = underlyingChannel.RemoteEndPoint;

			// Return the SslStream wrapped in our IChannel interface
			return new StreamBasedChannel(sslStream, remoteEndPoint);
		}
		catch (Exception)
		{
			this.logger.LogError("SSL handshake failed. Name: {Name}, Certificate: {Certificate}", name, certificate.Subject);
			sslStream.Close();
			throw;
		}
	}
}
