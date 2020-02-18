using System;
using System.Threading;

namespace ZipZip.Workers.DataBuffer
{
    internal partial class WaitersCollection
    {
        /// <summary>
        ///     This structure is needed for code self explanation
        /// </summary>
        public readonly struct WaitersMode
        {
            /// <summary>
            ///     Can be used for input buffer. Data can be taken for processing in any order
            /// </summary>
            public static WaitersMode OrdersDoesNotMatter()
            {
                return new WaitersMode(false, false, -1);
            }

            /// <summary>
            ///     Can be used for putting something in to output buffer. The pulling thread will be released only when appropriate
            ///     order will be added
            /// </summary>
            public static WaitersMode ReleaseWaiterByOrder(int order)
            {
                return new WaitersMode(true, false, order);
            }

            /// <summary>
            ///     Can be used to pull data from output buffer.
            /// </summary>
            /// <param name="order"></param>
            /// <returns></returns>
            public static WaitersMode CreateWaiterForOrder(int order)
            {
                return new WaitersMode(false, true, order);
            }

            private WaitersMode(bool orderMattersForReleasingAThread, bool orderMattersForWaiting, int order)
            {
                OrderMattersForReleasingAThread = orderMattersForReleasingAThread;
                OrderMattersForWaiting = orderMattersForWaiting;
                Order = order;
            }

            private bool OrderMattersForReleasingAThread { get; }
            private bool OrderMattersForWaiting { get; }
            private int Order { get; }

            /// <summary>
            ///     This is optimization (no GC). Every thread will have it's own WaitHandler.
            /// </summary>
            [ThreadStatic] private static ManualResetEvent ResetEventForThisThread;

            /// <summary>
            ///     Create a waiter for current thread. Later can be used to wait
            /// </summary>
            public WaitHandle CreateWaiter(WaitersCollection waitersCollection)
            {
                if (ResetEventForThisThread == null)
                    ResetEventForThisThread = new ManualResetEvent(false);
                else
                    ResetEventForThisThread.Reset();

                waitersCollection.CreateWaiter(OrderMattersForWaiting ? Order : (int?) null, ResetEventForThisThread);

                return ResetEventForThisThread;
            }

            /// <summary>
            ///     Releases a thread (if waiting. If not waiting - only removes  from waiters)
            /// </summary>
            /// <param name="waitersCollection"></param>
            public void ReleaseWaiter(WaitersCollection waitersCollection)
            {
                waitersCollection.ReleaseWaiter(OrderMattersForReleasingAThread ? Order : (int?) null);
            }
        }
    }
}