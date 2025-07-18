// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Application.Abstractions.Channels;
using AG.RouterService.SocksService.Infrastructure.Protocols;
using Microsoft.Extensions.Logging;

namespace AG.RouterService.SocksService.Infrastructure.Authentication;

internal sealed class UsernamePasswordAuthenticator : ISocks5Authenticator
{
	private const byte SubNegotiationVersion = 0x01;
	private readonly IUserValidator userValidator;
	private readonly ILogger<UsernamePasswordAuthenticator> logger;

	public UsernamePasswordAuthenticator(IUserValidator userValidator, ILogger<UsernamePasswordAuthenticator> logger)
	{
		this.userValidator = userValidator;
		this.logger = logger;
	}

	public byte Method => Socks5Constants.AuthMethodUsernamePassword;

	public async Task<bool> AuthenticateAsync(IChannel clientChannel, CancellationToken cancellationToken)
	{
		try
		{
			Memory<byte> ver = new byte[1];
			await clientChannel.ReadExactlyAsync(ver, cancellationToken);

			// Read ULEN and Username
			Memory<byte> ulenByte = new byte[1];
			await clientChannel.ReadExactlyAsync(ulenByte, cancellationToken);
			Memory<byte> unameBytes = new byte[ulenByte.Span[0]];
			await clientChannel.ReadExactlyAsync(unameBytes, cancellationToken);

			// Read PLEN and Password
			Memory<byte> plenByte = new byte[1];
			await clientChannel.ReadExactlyAsync(plenByte, cancellationToken);
			Memory<byte> passwdBytes = new byte[plenByte.Span[0]];
			await clientChannel.ReadExactlyAsync(passwdBytes, cancellationToken);

			string username = Encoding.ASCII.GetString(unameBytes.Span);
			string password = Encoding.ASCII.GetString(passwdBytes.Span);

			// Validate credentials
			bool isValid = await this.userValidator.ValidateAsync(username, password);

			// Send response: [VER, STATUS] (0x00 for success)
			byte status = isValid ? (byte)0x00 : (byte)0x01;
			byte[] response = { SubNegotiationVersion, status };
			await clientChannel.WriteAsync(response, cancellationToken);

			if (!isValid)
			{
				this.logger.LogWarning("SOCKS5 authentication failed for user: {Username}", username);
			}

			return isValid;
		}
		catch(EndOfStreamException)
		{
			this.logger.LogWarning("Client disconnected during authentication.");
			return false;
		}
	}
}
