// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class SslChannel : IChannel
{
	private readonly SslStream sslStream;

	public SslChannel(SslStream sslStream)
	{
		this.sslStream = sslStream;
	}

	public bool IsConnected => true; // SslStream does not expose a simple connected property. Assume true if instantiated.

	public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		return this.sslStream.ReadAsync(buffer, cancellationToken);
	}

	public async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		// SslStream, like other streams, may return partial data.
		// We must loop until the provided buffer is completely filled.
		Memory<byte> remainingBuffer = buffer;
		while (!remainingBuffer.IsEmpty)
		{
			int bytesRead = await this.sslStream.ReadAsync(remainingBuffer, cancellationToken);
			if (bytesRead == 0)
			{
				// The stream was closed before we could read all the requested bytes.
				throw new EndOfStreamException("The SSL stream was closed before the entire buffer could be filled.");
			}
			remainingBuffer = remainingBuffer.Slice(bytesRead);
		}
	}

	public async ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		await this.sslStream.WriteAsync(buffer, cancellationToken);
		return buffer.Length; // SslStream.WriteAsync does not return bytes written, assume all.
	}

	public async Task CloseAsync()
	{
		await this.sslStream.ShutdownAsync();
		this.sslStream.Close();
	}
}
