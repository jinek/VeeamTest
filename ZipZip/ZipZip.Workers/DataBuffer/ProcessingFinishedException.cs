using System;
using System.Runtime.Serialization;

namespace ZipZip.Workers.DataBuffer
{
    /// <summary>
    ///     Processing was finished. There is a need to abort worker threads (using instead of ThreadAbortException as not all debuggers can skip it)
    /// </summary>
    public class ProcessingFinishedException : Exception
    {
        public ProcessingFinishedException()
        {
        }

        // ReSharper disable once UnusedMember.Global using this for cross domain serialization
        protected ProcessingFinishedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}