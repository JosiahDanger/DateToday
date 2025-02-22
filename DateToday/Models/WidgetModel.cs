using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Timers;

namespace DateToday.Models
{
    public class WidgetModel : ReactiveObject
    {
        private readonly Timer _newMinuteEventGenerator;
        public IObservable<System.Reactive.EventPattern<ElapsedEventArgs>>? 
            NewMinuteEventObservable;

        public WidgetModel()
        {
            _newMinuteEventGenerator = new() { AutoReset = false };

            /* The Timer object will be configured such that it generates an Elapsed event every
             * minute, on the minute. My implementation of this behaviour relies on resetting its 
             * Interval property on each Elapsed event. This approach will prevent the Timer from 
             * drifting. */
            
            ResetTickGeneratorInterval();

            /* I have converted the Timer Elapsed event into a Rx.NET observable.
             * See: https://www.reactiveui.net/docs/handbook/events.html#how-do-i-convert-my-own-c-events-into-observables */

            NewMinuteEventObservable = 
                Observable.FromEventPattern<ElapsedEventHandler, ElapsedEventArgs>(
                    handler => _newMinuteEventGenerator.Elapsed += handler,
                    handler => _newMinuteEventGenerator.Elapsed -= handler
                );

            _newMinuteEventGenerator.Start();
        }

        public void ResetTickGeneratorInterval()
        {
            /* This function has been adapted from code posted to Stack Overflow by Jared. 
             * Thanks Jared!
             * See: https://stackoverflow.com/a/2075022 */

            DateTime currentDateTime = DateTime.Now;
            _newMinuteEventGenerator.Interval = 
                1000 * (60 - currentDateTime.Second) - currentDateTime.Millisecond;
        }
    }
}
