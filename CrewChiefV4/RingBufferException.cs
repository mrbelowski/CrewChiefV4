using System;
using System.Runtime.Serialization;

namespace RingBufferStream
{
    [Serializable]
    public sealed class RingBufferException : Exception
    {
        public RingBufferException()
        {
        }

        public RingBufferException(String message)
          : base(message)
        {
        }

        public RingBufferException(String message, Exception exception)
          : base(message, exception)
        {
        }

        private RingBufferException(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
        }
    }
}