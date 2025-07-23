// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;

namespace AG.RouterService.DomainNameService.Domain;

public class HostRecord
{
	public string Hostname { get; set; } = string.Empty;
	public List<string> IpAddress { get; set; } = new();
}
