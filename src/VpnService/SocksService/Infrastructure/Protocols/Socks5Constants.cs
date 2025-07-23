// Copyright By Hossein Azizollahi All Right Reserved.

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal static class Socks5Constants
{
	public const byte Version = 0x05;

	// Authentication Methods
	public const byte AuthMethodNoAuthentication = 0x00;
	public const byte AuthMethodUsernamePassword = 0x02;

	// Commands
	public const byte CommandConnect = 0x01;
	public const byte CommandBind = 0x02;
	public const byte CommandUdpAssociate = 0x03;

	// Address Types
	public const byte AddressTypeIPv4 = 0x01;
	public const byte AddressTypeDomainName = 0x03;
	public const byte AddressTypeIPv6 = 0x04;

	// Reply Codes
	public const byte ReplySucceeded = 0x00;
	public const byte ReplyGeneralFailure = 0x01;
	public const byte ReplyConnectionNotAllowed = 0x02;
	public const byte ReplyNetworkUnreachable = 0x03;
	public const byte ReplyHostUnreachable = 0x04;
	public const byte ReplyConnectionRefused = 0x05;
	public const byte ReplyTtlExpired = 0x06;
	public const byte ReplyCommandNotSupported = 0x07;
	public const byte ReplyAddressTypeNotSupported = 0x08;
}
