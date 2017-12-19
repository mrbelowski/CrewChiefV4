using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RingBufferStream
{
    //http://en.wikipedia.org/wiki/Circular_buffer
    public class RingBufferStream : MemoryStream
    {
        private static readonly int _pageSize = Environment.SystemPageSize / 4;

        private bool _wrapping;
        private long _headPosition;
        private long _tailPosition;

        public RingBufferStream()
            : this(1024)
        {
        }

        public RingBufferStream(int capacity)
            : base(capacity)
        {
            _headPosition = 0; //Next read position
            _tailPosition = 0; //Next write position
            CanGrow = true;
        }

        public bool CanGrow { get; set; }

        public override int Capacity
        {
            set
            {
                if (value > Capacity && !CanGrow)
                {
                    throw new RingBufferException("Buffer capacity growth not allowed");
                }
                int increase = value - Capacity;
                if (_wrapping)
                {
                    //The capacity must be increased by a minimum of at least the tail position
                    if (_tailPosition > increase)
                    {
                        increase = (int)_tailPosition;
                    }
                    base.Capacity += increase;
                    //Keep increases rounded up to chunks of system page size (for better performance)
                    base.Capacity = _pageSize * ((Capacity / _pageSize) + 1);
                    GrowBuffer();
                    return;
                }

                base.Capacity += increase;
                base.Capacity = _pageSize * ((Capacity / _pageSize) + 1);
            }
        }

        public override long Length
        {
            get
            {
                if (_wrapping)
                {
                    return (base.Length - _headPosition) + _tailPosition;
                }
                return (_tailPosition - _headPosition);
            }
        }

        public override long Position
        {
            get
            {
                //Interesting, this is always zero since reading always happens at the head position
                //Thus this needs to be used carefully and should take into consideration the Length property
                return 0;
            }
            set
            {
                if (_headPosition != value)
                {
                    base.Position = Seek(value - _headPosition, SeekOrigin.Begin);
                    _wrapping = _headPosition > _tailPosition;
                }
            }
        }

        protected long HeadPosition
        {
            get { return _headPosition; }
        }

        protected long TailPosition
        {
            get { return _tailPosition; }
        }

        public override void SetLength(long value)
        {
            if (value == 0)
            {
                _headPosition = 0;
                _tailPosition = 0;
                _wrapping = false;
            }
            else
            {
                if (value < Length)
                {
                    long difference = Length - value;
                    if (_wrapping && _tailPosition < difference)
                    {
                        difference -= _tailPosition;
                        _tailPosition = value - difference;
                        _wrapping = false;
                    }
                    else
                    {
                        _tailPosition -= difference;
                    }
                    if (_tailPosition < _headPosition)
                    {
                        _headPosition = _tailPosition;
                    }
                }
            }
            base.SetLength(value);
        }

        public IList<ArraySegment<byte>> GetBuffer(int count)
        {
            #region //Sanity Checking

            if (!CalibrateHead(false) && count > 0 || count > Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            #endregion

            var buffers = new List<ArraySegment<byte>>();
            if (_wrapping && _headPosition + count > Capacity) //Check whether request for buffer needs to wrap around
            {
                var remainder = (int)(Capacity - _headPosition);
                buffers.Add(new ArraySegment<byte>(GetBuffer(), (int)_headPosition, remainder));
                buffers.Add(new ArraySegment<byte>(GetBuffer(), 0, count - remainder));
            }
            else
            {
                buffers.Add(new ArraySegment<byte>(GetBuffer(), (int)_headPosition, count));
            }
            return buffers;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            #region //Sanity Checking

            if (!CalibrateHead())
            {
                return 0;
            }
            var length = (int)Length; //Length of the ring buffer
            //Limit the number of bytes to read to the length of available bytes
            if (count > length)
            {
                count = length;
            }
            //Must check the count after the length limit was applied
            if (count == 0)
            {
                return 0;
            }

            #endregion

            int read;
            if (_wrapping && _headPosition + count > Capacity)
            {
                var remainder = (int)(Capacity - _headPosition);
                read = base.Read(buffer, offset, remainder);
                base.Position = 0;
                read += base.Read(buffer, offset + read, count - read);
                _wrapping = false;
            }
            else
            {
                read = base.Read(buffer, offset, count);
            }
            _headPosition = base.Position;
            return read;
        }

        private bool CalibrateHead(bool setPosition = true)
        {
            if (!_wrapping && _headPosition == _tailPosition)
            {
                if (setPosition)
                {
                    base.Position = _headPosition;
                }
                return false;
            }
            if (_headPosition == Capacity)
            {
                _headPosition = 0;
                _wrapping = false;
            }
            if (setPosition)
            {
                base.Position = _headPosition;
            }
            return true;
        }

        public override int ReadByte()
        {
            if (!CalibrateHead())
            {
                return -1;
            }
            ++_headPosition;
            return base.ReadByte();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            var length = (int)Length + count; //Required length of the ring buffer
            if (length > Capacity)
            {
                Capacity = length;
            }

            CalibrateTail();

            if (_wrapping || _tailPosition + count <= Capacity)
            {
                //If the tail has wrapped it is safe to assume there is enough space between the tail and head,
                // otherwise the length check above would have grown the internal buffer and "unwrapped" the tail
                base.Write(buffer, offset, count);
            }
            else
            {
                var remainder = (int)(Capacity - _tailPosition);
                base.Write(buffer, offset, remainder);
                base.Position = 0;
                base.Write(buffer, offset + remainder, count - remainder);
                _wrapping = true;
            }
            _tailPosition = base.Position;
        }

        public override void WriteByte(byte value)
        {
            if (_wrapping && _tailPosition == _headPosition)
            {
                Capacity += 1;
            }
            CalibrateTail();
            base.WriteByte(value);
            _tailPosition = base.Position;
        }

        private void CalibrateTail(bool setPosition = true)
        {
            if (_tailPosition == Capacity)
            {
                _tailPosition = 0;
                if (_headPosition == Capacity)
                    _headPosition = 0;
                else
                    _wrapping = true;
            }
            if (setPosition)
            {
                base.Position = _tailPosition;
            }
        }

        private void GrowBuffer()
        {
            if (!CanGrow)
            {
                throw new RingBufferException("Buffer full and grow not allowed");
            }

            var count = (int)(_tailPosition);
            Debug.Assert(count > 0);

            var offset = (int)base.Length;
            //SetLength will grow the underlying buffer and doubling the capacity (if needed)
            base.SetLength(base.Length + count);
            Buffer.BlockCopy(base.GetBuffer(), 0, base.GetBuffer(), offset, count);
            _tailPosition = base.Length;
            _wrapping = false;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (loc == SeekOrigin.Begin || loc == SeekOrigin.Current)
            {
                #region //Sanity Checking

                if (!CalibrateHead(false) && offset > 0)
                {
                    throw new ArgumentOutOfRangeException("offset");
                }
                if (offset == 0)
                {
                    return _headPosition;
                }
                //Check for any attempt to move the head past the tail
                if (_wrapping && offset < 0 && _headPosition + offset < _tailPosition)
                {
                    throw new ArgumentOutOfRangeException("offset");
                }
                if (offset > 0 && offset > Length)
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                #endregion

                long position = _headPosition + offset;
                if (_wrapping && position > Capacity)
                {
                    //Head has now wrapped and the tail was wrapped, thus adjust the relative position
                    position = position - Capacity;
                    if (position > _tailPosition)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    //Since the head has wrapped the wrapping status can be reset
                    _wrapping = false;
                    return _headPosition = position;
                }
                if (position < 0)
                {
                    position = Capacity + position;
                    if (position < _tailPosition)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    _wrapping = true;
                    return _headPosition = position;
                }
                return _headPosition = position;
            }

            if (loc == SeekOrigin.End)
            {
                #region //Sanity Checking

                CalibrateTail(false);
                if (offset == 0)
                {
                    return _tailPosition;
                }
                //Check for any attempt to move the tail past the head
                if (_wrapping && offset > 0 && _tailPosition + offset > _headPosition)
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                #endregion

                long position = _tailPosition + offset;
                if (position < 0)
                {
                    //Tail has backtracked past the head, thus unwrapping it
                    position = Capacity + position;
                    if (position < _headPosition)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    _wrapping = false;
                    return _tailPosition = position;
                }
                if (position > Capacity)
                {
                    //Tail was moved forward and has now wrapped, thus adjust the relative position
                    position = position - Capacity;
                    if (position > _headPosition)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    _wrapping = true;
                    return _tailPosition = position;
                }
                return _tailPosition = position;
            }

            throw new ArgumentOutOfRangeException("loc");
        }

        public override byte[] ToArray()
        {
            var copy = new byte[Length];
            Read(copy, 0, copy.Length);
            return copy;
        }
    }
}