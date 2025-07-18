// Copyright By Hossein Azizollahi All Right Reserved.

using System;

namespace AG.RouterService.SocksService.Application.Abstractions.Exceptions;

public class UnsupportedAddressTypeException : Exception
{
	public byte AddressType { get; }

	public UnsupportedAddressTypeException(byte addressType)
		: base($"SOCKS5 address type '{addressType:X2}' is not supported.")
	{
		this.AddressType = addressType;
	}
}
