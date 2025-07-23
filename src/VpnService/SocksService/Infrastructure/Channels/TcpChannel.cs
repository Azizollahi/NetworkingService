// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class TcpChannel : IChannel
{
	private readonly Socket socket;
	public IPEndPoint RemoteEndPoint => (IPEndPoint)socket.RemoteEndPoint!;

	public TcpChannel(Socket socket)
	{
		this.socket = socket;
	}

	public bool IsConnected => this.socket.Connected;

	public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		return this.socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
	}

	public async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		Memory<byte> remainingBuffer = buffer;
		while (!remainingBuffer.IsEmpty)
		{
			int bytesRead = await ReadAsync(remainingBuffer, cancellationToken);
			if (bytesRead == 0)
			{
				throw new EndOfStreamException("The channel was closed before the entire buffer could be filled.");
			}
			remainingBuffer = remainingBuffer[bytesRead..];
		}
	}

	public async ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		int totalBytesWritten = 0;
		ReadOnlyMemory<byte> remainingBuffer = buffer;

		while (!remainingBuffer.IsEmpty)
		{
			int bytesWritten = await this.socket.SendAsync(remainingBuffer, SocketFlags.None, cancellationToken);
			if (bytesWritten == 0)
			{
				// This case can indicate a closed or broken connection.
				// Breaking the loop is a safe default.
				break;
			}
			totalBytesWritten += bytesWritten;
			remainingBuffer = remainingBuffer[bytesWritten..];
		}

		return totalBytesWritten;
	}

	public Task CloseAsync()
	{
		if (this.socket.Connected)
		{
			this.socket.Shutdown(SocketShutdown.Both);
			this.socket.Close();
		}
		return Task.CompletedTask;
	}
}
