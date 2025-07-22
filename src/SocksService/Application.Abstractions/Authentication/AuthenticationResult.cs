// Copyright By Hossein Azizollahi All Right Reserved.

using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Application.Abstractions.Authentication;

public class AuthenticationResult
{
	public bool IsSuccess { get; }
	public IChannel Channel { get; }
	public string? Username { get; } // Add this property

	private AuthenticationResult(bool isSuccess, IChannel channel, string? username)
	{
		IsSuccess = isSuccess;
		Channel = channel;
		Username = username;
	}

	public static AuthenticationResult Success(IChannel channel, string? username = null) => new(true, channel, username);
	public static AuthenticationResult Failure(IChannel channel) => new(false, channel, null);
}
