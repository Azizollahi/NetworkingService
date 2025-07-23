// Copyright By Hossein Azizollahi All Right Reserved.

namespace AG.RouterService.SocksService.Infrastructure.Protocols;

internal static class Socks4Constants
{
	public const byte Version = 0x04;

	// Commands
	public const byte CommandConnect = 0x01;
	public const byte CommandBind = 0x02;

	// Reply Codes
	public const byte ReplyGranted = 0x5a;
	public const byte ReplyFailed = 0x5b;
}
