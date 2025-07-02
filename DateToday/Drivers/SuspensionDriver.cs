using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using static DateToday.Utilities;

namespace DateToday.Drivers
{
    internal sealed class SuspensionDriver<T>(string filepath) : ISuspensionDriver
    {
        public IObservable<Unit> InvalidateState()
        {
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<object> LoadState()
        {
            /* This implementation has been adapted from an example provided in the ReactiveUI 
             * handbook. See: https://www.reactiveui.net/docs/handbook/data-persistence.html
             * 
             * Unfortunately, the example code raises error CS8619, identifying that the nullability 
             * of the returned object does not match that of the target return type.
             * 
             * However, when the returned object is actually null, the ReactiveUI data persistence 
             * functionality is smart enough to handle the resulting exception, and instead 
             * retrieves a default application state from CreateNewAppState(). Therefore, I have 
             * opted to use the null-forgiving operator here. */

            object? appState = DeserialiseFile<T>(filepath);

            return Observable.Return(appState)!;
        }

        public IObservable<Unit> SaveState(object state)
        {
            string jsonText = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(filepath, jsonText);

            return Observable.Return(Unit.Default);
        }
    }
}
