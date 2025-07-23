// Copyright By Hossein Azizollahi All Right Reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Channels;

namespace AG.RouterService.SocksService.Infrastructure.Channels;

internal sealed class ChannelStream : Stream
{
	private readonly IChannel channel;

	public ChannelStream(IChannel channel)
	{
		this.channel = channel;
	}

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => true;
	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return await this.channel.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
	}

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		await this.channel.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
	}

	public override void Flush() { }
	public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	public override int Read(byte[] buffer, int offset, int count) => this.ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
	public override void Write(byte[] buffer, int offset, int count) => this.WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.channel.CloseAsync().GetAwaiter().GetResult();
		}
		base.Dispose(disposing);
	}
}
