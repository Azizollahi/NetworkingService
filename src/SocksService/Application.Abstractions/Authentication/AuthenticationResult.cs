// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Authentication;

public class AuthenticationResult
{
	public bool IsSuccess { get; }
	public IChannel Channel { get; }

	private AuthenticationResult(bool isSuccess, IChannel channel)
	{
		IsSuccess = isSuccess;
		Channel = channel;
	}

	public static AuthenticationResult Success(IChannel channel) => new(true, channel);
	public static AuthenticationResult Failure(IChannel channel) => new(false, channel);
}
