// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Application.Abstractions.Exceptions;
using AG.RouterService.SocksService.Application.Abstractions.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal sealed class Socks5AddressReader : ISocks5AddressReader
{
	private readonly ILogger<Socks5AddressReader> logger;

	public Socks5AddressReader(ILogger<Socks5AddressReader> logger)
	{
		this.logger = logger;
	}

	public async Task<(string? Host, int Port)> ReadAddressAndPortAsync(
		IChannel clientChannel,
		ReadOnlyMemory<byte> commandHeader,
		CancellationToken cancellationToken)
	{
		try
		{
			string host;
			byte addressType = commandHeader.Span[3];

			switch (addressType)
			{
				case Socks5Constants.AddressTypeIPv4:
					Memory<byte> ipv4Bytes = new byte[4];
					await clientChannel.ReadExactlyAsync(ipv4Bytes, cancellationToken);
					host = new IPAddress(ipv4Bytes.ToArray()).ToString();
					break;

				case Socks5Constants.AddressTypeDomainName:
					Memory<byte> domainLengthByte = new byte[1];
					await clientChannel.ReadExactlyAsync(domainLengthByte, cancellationToken);

					Memory<byte> domainBytes = new byte[domainLengthByte.Span[0]];
					await clientChannel.ReadExactlyAsync(domainBytes, cancellationToken);
					host = Encoding.ASCII.GetString(domainBytes.Span);
					break;

				case Socks5Constants.AddressTypeIPv6:
					Memory<byte> ipv6Bytes = new byte[16];
					await clientChannel.ReadExactlyAsync(ipv6Bytes, cancellationToken);
					host = new IPAddress(ipv6Bytes.ToArray()).ToString();
					break;

				default:
					throw new UnsupportedAddressTypeException(addressType);
			}

			Memory<byte> portBytes = new byte[2];
			await clientChannel.ReadExactlyAsync(portBytes, cancellationToken);
			int port = (portBytes.Span[0] << 8) | portBytes.Span[1];

			this.logger.LogDebug("Parsed SOCKS5 destination: {Host}:{Port}", host, port);
			return (host, port);
		}
		catch (EndOfStreamException ex)
		{
			this.logger.LogWarning(ex, "Client closed connection while reading address information.");
			return (null, 0);
		}
	}
}
