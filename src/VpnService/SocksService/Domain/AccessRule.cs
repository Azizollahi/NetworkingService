// Copyright By Hossein Azizollahi All Right Reserved.

namespace AG.RouterService.SocksService.Domain;

public enum RuleType
{
	Allow,
	Deny
}

public enum TargetType
{
	SourceIp,
	DestinationHost
}

public class AccessRule
{
	public RuleType Type { get; set; }
	public TargetType Target { get; set; }
	public string Pattern { get; set; } = string.Empty;
}
