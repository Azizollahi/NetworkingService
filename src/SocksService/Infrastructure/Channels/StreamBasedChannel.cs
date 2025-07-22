// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class StreamBasedChannel : IChannel
{
	private readonly Stream stream;

	public IPEndPoint RemoteEndPoint { get; }

	public StreamBasedChannel(Stream stream, IPEndPoint remoteEndPoint)
	{
		this.stream = stream;
		this.RemoteEndPoint = remoteEndPoint;
	}

	// SslStream/NegotiateStream don't expose a simple connected property.
	// We assume true if instantiated, and the first read/write will fail if disconnected.
	public bool IsConnected => true;

	public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		return this.stream.ReadAsync(buffer, cancellationToken);
	}

	public async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		Memory<byte> remainingBuffer = buffer;
		while (!remainingBuffer.IsEmpty)
		{
			int bytesRead = await this.stream.ReadAsync(remainingBuffer, cancellationToken);
			if (bytesRead == 0)
			{
				throw new EndOfStreamException("The underlying stream was closed before the entire buffer could be filled.");
			}
			remainingBuffer = remainingBuffer.Slice(bytesRead);
		}
	}

	public async ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		await this.stream.WriteAsync(buffer, cancellationToken);
		return buffer.Length;
	}

	public async Task CloseAsync()
	{
		await this.stream.DisposeAsync();
	}
}
