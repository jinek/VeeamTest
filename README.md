_Please, check scheme.png for short description._
---
Warning! When compiling - use "RELEASE" mode to see user message in case of unexpected errors.
---
###Description
In short, we use one thread for reading the file (idling while reading, see p.s.).
Then it pushes chunk to input buffer.
Then it goes to idle if buffer contains too much of (unprocessed) data.

Then, there are several working threads (number of logical processors).
Each of this thread takes a chunk and process it.
If buffer is empty this worker-thread also goes to idle.
After processing it pushes data to output buffer.
And finally, if output buffer is overflowed, this worker thread locks (idle) again.

And last, outputting thread (which writes output file), tries to pull a processed chunk from output buffer.
If there is no specific chunk, it locks and waits until that specific chunk will appear in the output buffer.
Finally, it writes chunk to file.
---
### Addition
As seen, from the scheme, we have two buffers.
They can be accessed from several threads, which means they can lock and release those threads.
Using this locks/idling we are releasing a core (logical processor),
when it's not used.
This is self-balanced construction of cross-locking threads.

Example: input thread is locked on input buffer because it reached its capacity. Worker threads don't take data from input buffer because they are locked on output buffer. And Output thread does not take data from output thread, because it's idled on write-file operation.
Thus, all threads of out application are locked on file operation and none of them use CPU, until file operation is completed.  

---
p.s.
Because of it is not allowed to use threadpool, we can not used Stream.BeginRead/BegingWrite. But instead we need to use Read/Write which makes current thread idling.
That means, that two threads (input from file and output to file) will stay idle for some time.
And this is the reason, why we have number of worker threads = logical processors number.
