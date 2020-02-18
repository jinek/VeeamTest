using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace ZipZip.Exceptions
{
    public class UserErrorException : ApplicationException
    {
        public UserErrorException(string message) : base(message)
        {
        }

        // ReSharper disable once UnusedMember.Global
        protected UserErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        [DebuggerStepThrough]
        public static void ThrowUserErrorException(string message)
        {
            throw new UserErrorException(message);
        }
    }
}