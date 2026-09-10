using DateToday.Avalonia.Resources;
using System;
using System.Threading;

namespace DateToday.Utilities;

internal static class ActionScheduler
{
	/// <summary>
	/// Creates a timer that performs a specified action at regular intervals aligned to real-time
	/// clock boundaries.
	/// </summary>
	/// <remarks>
	/// The timer delays execution of its first action until the next interval boundary occurs.
	/// Subsequent executions will continue at the specified interval.
	/// </remarks>
	/// <param name="intervalSeconds">
	/// An interval in seconds between successive executions of the specified action. Must be
	/// greater than zero. Intended range: 1-86400 (one second to one day).
	/// </param>
	/// <param name="onTick">
	/// An action to perform at the specified interval.
	/// </param>
	/// <returns>
	/// A configured <see cref="System.Threading.Timer"/> that must be disposed of by the caller.
	/// </returns>
	/// <exception cref="NotSupportedException">
	/// Thrown if no <see cref="SynchronizationContext"/> is available on the current thread.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown if <paramref name="refreshIntervalSeconds"/> is zero.
	/// </exception>

	public static Timer Create(uint intervalSeconds, Action onTick)
	{
		SynchronizationContext synchronisationContext = SynchronizationContext.Current
			?? throw new NotSupportedException(
				Strings.ActionScheduler_Exception_SynchronizationContext_Null);

		if (intervalSeconds == 0)
		{
			throw new InvalidOperationException(
				Strings.ActionScheduler_Exception_RefreshIntervalSeconds_Zero);
		}

		long millisecondsPerInterval = intervalSeconds * TimeSpan.MillisecondsPerSecond;

		long totalMillisecondsSucceedingEpoch =
			DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;       // My money long
																		// My pockets deep
		long millisecondsSucceedingPreviousInterval =
			totalMillisecondsSucceedingEpoch % millisecondsPerInterval;

		long millisecondsPrecedingNextInterval =
			(millisecondsPerInterval - millisecondsSucceedingPreviousInterval)
			% millisecondsPerInterval;

		// Check if the next action execution is scheduled to occur right now.

		if (millisecondsPrecedingNextInterval == 0)
		{
			/* The timer must wait for the next interval boundary to occur before executing an
			 * action. This behaviour is intended to prevent a scenario in which two actions are
			 * executed in close succession of one another. */

			millisecondsPrecedingNextInterval = millisecondsPerInterval;
		}

		return new Timer(
			_ =>
				synchronisationContext.Post(_ => onTick(), null),
				null,
				millisecondsPrecedingNextInterval,
				millisecondsPerInterval);
	}
}
