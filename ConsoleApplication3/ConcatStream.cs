using System;
using System.IO;

namespace CS422
{
	public class ConcatStream : Stream
	{
		Stream _firstStream;
		Stream _secondStream;
		long _fixedLength;
		long _firstPosition;
		long _secondPosition;

		public ConcatStream(Stream first, Stream second)
		{
			this._firstStream = first;
			this._secondStream = second;

			// Try to grab both lengths. If the first
			try
			{
				long testVar = this._firstStream.Length;
			}
			catch
			{
				throw new ArgumentException();
			}

			this._fixedLength = -1;
            this._firstPosition = 0;
			this._secondPosition = 0;
		}

		public ConcatStream(Stream first, Stream second, long fixedLength)
		{
			this._firstStream = first;
			this._secondStream = second;
			this._fixedLength = fixedLength;
			SetLength (this._fixedLength);
			this._firstPosition = 0;
			this._secondPosition = 0;
		}

		public override bool CanRead
		{
			get
			{
				if (_firstStream.CanRead == true &&
					_secondStream.CanRead == true)
				{
					return true;
				}

				return false;
			}
		}

		public override bool CanSeek
		{
			get
			{
				if (_firstStream.CanSeek == true &&
					_secondStream.CanSeek == true)
				{
					return true;
				}

				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				if (_firstStream.CanWrite == true &&
					_secondStream.CanWrite == true)
				{
					return true;
				}

				return false;
			}
		}

		public override long Length
		{
			get
			{
                if (this._fixedLength >= 0)
                {
                    return this._fixedLength;
                }
                else
                {
					throw new NotSupportedException ();
                }
			}
		}

		public override void SetLength(long value)
		{
			if (this._fixedLength != -1) 
			{
				this._fixedLength = value;
			} 
		}

		public override long Position
		{
			get
			{
				if (this._secondPosition > 0) 
				{
					return this._secondPosition + this._firstPosition;
				}

				return this._firstPosition;
			}

			set
			{
				if (value < this._firstStream.Length) 
				{
					this._firstPosition = value;
				} 
				else 
				{
					this._firstPosition = this._firstStream.Length;
					this._secondPosition = this._firstStream.Length - value;
				}
			}
		}

		public override void Flush()
		{
			this._firstStream.Flush();
			this._secondStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			// Make sure both of the stream can be read from.
			// If not return an exception.
			if (!CanRead)
			{
				throw new NotSupportedException();
			}

			// If the second stream cannot seek, only allow
			// forward reading.
			if (!this._secondStream.CanSeek) 
			{
				if (count < 0) 
				{
					throw new NotSupportedException ();
				}
			}

			int firstNumBytes = this._firstStream.Read (buffer, offset, count);
			this._firstPosition += firstNumBytes;

			int secondNumBytes = this._secondStream.Read (buffer, offset + firstNumBytes, count - firstNumBytes);
			this._secondPosition += secondNumBytes;

			return firstNumBytes + secondNumBytes;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			// If we can't seek just throw an exception
			if (!CanSeek)
			{
				throw new NotSupportedException();
			}

			// Are we starting at the beginning of the stream?
			if (origin == SeekOrigin.Begin) 
			{
				// If so, are we trying to offset to the first or the second stream?
				if (offset < this._firstStream.Length) 
				{
					// The seek fits into the first stream, so set the position
					// to that and reset the second streams position to 0.
					this._firstPosition = offset;
					this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);

					this._secondPosition = 0;
					this._secondStream.Seek (this._secondPosition, SeekOrigin.Begin);
				} 
				else
				{
					// The seek needs to go to the second stream.
					// First set the first stream's position to the end.
					// Calculate how much to take off of the offset
					// Set the second streams position to that new offset.
					this._firstStream.Seek (0, SeekOrigin.End);
                    this._firstPosition = this._firstStream.Position;

					this._secondPosition = offset - this._firstStream.Length;
					this._secondStream.Seek (this._secondPosition, SeekOrigin.Begin);
				}
			}

			// Are we starting where we last left off?
			if (origin == SeekOrigin.Current) 
			{
				// If we were in the second stream, then we need to
				// add our new offset to that stream. Otherwise, we 
				// need to start from the first stream and see if
				// we are going to seek into the second.
				if (this._secondPosition != 0) 
				{
					if (offset < 0) 
					{
						if (this._secondPosition - offset < 0) 
						{
							this._firstPosition -= (this._secondPosition - offset);
							this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);
							this._secondPosition = 0;
							this._secondStream.Seek (0, SeekOrigin.Begin);

						} 
						else 
						{
							this._firstPosition = this._firstStream.Length;
							this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);
							this._secondPosition -= offset;
							this._secondStream.Seek (this._secondPosition, SeekOrigin.Begin);
						}
					} 
					else 
					{
						this._firstPosition = this._firstStream.Length;
						this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);
						this._secondPosition += offset;
						this._secondStream.Seek (this._secondPosition, SeekOrigin.Begin);
					}
				} 
				else 
				{
					// Check to see if this week will go beyond the first stream.
					// If not then 
					if ((this._firstPosition + offset) < this._firstStream.Length) 
					{
						this._firstPosition += offset;
						this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);
						this._secondPosition = 0;
						this._secondStream.Seek (0, SeekOrigin.Begin);
					} 
					else 
					{
						this._secondPosition = offset - (this._firstStream.Length - this._firstPosition);
						this._firstPosition = this._firstStream.Length;

						this._firstStream.Seek (0, SeekOrigin.End);
						this._secondStream.Seek (this._secondPosition, SeekOrigin.Begin);
					}
				}
			}

			// Are we starting where we last left off?
			if (origin == SeekOrigin.End) 
			{
				// First in order to get the end, the second stream has to have
				// a length. If there was no length from earlier, fixedLength
				// will equal -1 so just check that.
				if (this._fixedLength == -1) 
				{
					throw new NotSupportedException ();
				}

				// If we are reading in a negative direction, we need to
				// see if our seek will bring us to the first stream. If 
				// it's positive then they will seek past the length of
				// the Stream, which does not make sense to me, but that 
				// is what the documentation said.
				if (offset < 0) 
				{
					// If the offset is big enough to take us into the first
					// stream, calculate where it will leave us then do the
					// appropriate seeking.
					if ((this._secondStream.Length - offset) < 0) 
					{
						this._firstPosition = this._fixedLength - offset;

						this._firstStream.Seek (this._firstPosition, SeekOrigin.Begin);
						this._secondStream.Seek (0, SeekOrigin.Begin);
					} 
					else 
					{
						this._secondPosition = this._fixedLength - offset;

						this._firstStream.Seek (0, SeekOrigin.End);
						this._secondStream.Seek (this._secondPosition, SeekOrigin.End);

					}
				} 
				else 
				{
					this._secondStream.Seek (offset, SeekOrigin.End);
					this._firstStream.Seek (0, SeekOrigin.End);
				}


			}

			return Position;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			// If both streams aren't able to write, throw
			// the NotSupportExcetion.
			if (!CanWrite)
			{
				throw new NotSupportedException();
			}

			// Calculate how much space is left in the first stream
			// for the write (don't want to write more than what room we have).
			int firstWrite = (int)this._firstStream.Length - (int)this._firstPosition;

			// If we are writing something smaller then how much space is left
			// just switch to count instead.
			if (count < firstWrite) 
			{
				firstWrite = count;
			} 

			// Go ahead and write however much we can into stream 1
			// then increment the position of our stream.
			this._firstStream.Write (buffer, offset, firstWrite);
			this._firstPosition += firstWrite;

			// Figure out how much was not written then write it to stream 2
			// and increment the position of our stream by that much.
			int secondWrite = count - firstWrite;
			this._secondStream.Write (buffer, offset + firstWrite, secondWrite);
			this._secondPosition += secondWrite;

		}
	}
}

