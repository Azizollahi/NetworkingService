// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class SplitPortChannel : IChannel
	{
		private readonly Socket readSocket;
		private readonly Socket writeSocket;

		public IPEndPoint RemoteEndPoint => (IPEndPoint)readSocket.RemoteEndPoint!;

		public SplitPortChannel(Socket readSocket, Socket writeSocket)
		{
			this.readSocket = readSocket;
			this.writeSocket = writeSocket;
		}

		public bool IsConnected => readSocket.Connected && writeSocket.Connected;

		public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return readSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
		}

		public async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			Memory<byte> remainingBuffer = buffer;
			while (!remainingBuffer.IsEmpty)
			{
				int bytesRead = await readSocket.ReceiveAsync(remainingBuffer, SocketFlags.None, cancellationToken);
				if (bytesRead == 0)
				{
					throw new EndOfStreamException("The read socket was closed before the entire buffer could be filled.");
				}
				remainingBuffer = remainingBuffer.Slice(bytesRead);
			}
		}

		public async ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			int totalBytesWritten = 0;
			ReadOnlyMemory<byte> remainingBuffer = buffer;
			while (!remainingBuffer.IsEmpty)
			{
				int bytesWritten = await writeSocket.SendAsync(remainingBuffer, SocketFlags.None, cancellationToken);
				if (bytesWritten == 0)
				{
					break;
				}
				totalBytesWritten += bytesWritten;
				remainingBuffer = remainingBuffer.Slice(bytesWritten);
			}
			return totalBytesWritten;
		}

		public Task CloseAsync()
		{
			try
			{
				if (readSocket.Connected)
				{
					readSocket.Shutdown(SocketShutdown.Both);
				}
			}
			finally
			{
				readSocket.Close();
			}

			try
			{
				if (this.writeSocket.Connected)
				{
					this.writeSocket.Shutdown(SocketShutdown.Both);
				}
			}
			finally
			{
				this.writeSocket.Close();
			}

			return Task.CompletedTask;
		}
	}
