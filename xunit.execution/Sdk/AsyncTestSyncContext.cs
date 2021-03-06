﻿using System;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS_PHONE_APP
using Windows.System.Threading;
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// This implementation of <see cref="SynchronizationContext"/> allows the developer to track the count
	/// of outstanding "async void" operations, and wait for them all to complete.
	/// </summary>
	public class AsyncTestSyncContext : SynchronizationContext
	{
		private readonly AsyncManualResetEvent @event = new AsyncManualResetEvent(true);
		private Exception exception;
		private readonly SynchronizationContext innerContext;
		private int operationCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncTestSyncContext"/> class.
		/// </summary>
		/// <param name="innerContext">The existing synchronization context (may be <c>null</c>).</param>
		public AsyncTestSyncContext(SynchronizationContext innerContext)
		{
			this.innerContext = innerContext;
		}

		/// <inheritdoc/>
		public override void OperationCompleted()
		{
			var result = Interlocked.Decrement(ref operationCount);
			if (result == 0)
				@event.Set();
		}

		/// <inheritdoc/>
		public override void OperationStarted()
		{
			Interlocked.Increment(ref operationCount);
			@event.Reset();
		}

		/// <inheritdoc/>
		public override void Post(SendOrPostCallback d, object state)
		{
			// The call to Post() may be the state machine signaling that an exception is
			// about to be thrown, so we make sure the operation count gets incremented
			// before the Task.Run, and then decrement the count when the operation is done.
			OperationStarted();

			try
			{
				if (innerContext == null)
				{
					XunitWorkerThread.QueueUserWorkItem(() =>
					{
						try
						{
							Send(d, state);
						}
						finally
						{
							OperationCompleted();
						}
					});
				}
				else
					innerContext.Post(_ =>
					{
						try
						{
							Send(d, _);
						}
						finally
						{
							OperationCompleted();
						}
					}, state);
			}
			catch { }
		}

		/// <inheritdoc/>
		public override void Send(SendOrPostCallback d, object state)
		{
			try
			{
				if (innerContext != null)
					innerContext.Send(d, state);
				else
					d(state);
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		}

		/// <summary>
		/// Returns a task which is signaled when all outstanding operations are complete.
		/// </summary>
		public async Task<Exception> WaitForCompletionAsync()
		{
			await @event.WaitAsync();

			return exception;
		}
	}
}