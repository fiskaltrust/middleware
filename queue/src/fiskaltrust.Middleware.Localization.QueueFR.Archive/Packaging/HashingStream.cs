using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Packaging
{
    /// <summary>A write-only pass-through stream that computes the SHA-256 and the length of what goes through it.</summary>
    internal sealed class HashingStream : Stream
    {
        private readonly Stream _inner;
        private readonly bool _leaveOpen;
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private byte[]? _result;
        private long _length;

        public HashingStream(Stream inner, bool leaveOpen)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _leaveOpen = leaveOpen;
        }

        public long BytesWritten => _length;

        /// <summary>The SHA-256 of everything written. Writing is closed once it is read.</summary>
        public byte[] GetHash() => _result ??= _hash.GetHashAndReset();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => _result is null;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _length;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Append(buffer, offset, count);
            _inner.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Append(buffer, offset, count);
            await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hash.Dispose();
                if (!_leaveOpen)
                {
                    _inner.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void Append(byte[] buffer, int offset, int count)
        {
            if (_result is not null)
            {
                throw new InvalidOperationException("The hash has already been taken.");
            }
            _hash.AppendData(buffer, offset, count);
            _length += count;
        }
    }
}
