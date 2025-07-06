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
            /* There is absolutely no need to first verify that the target file exists before 
             * attempting to delete it. No exceptions will be raised either way. */
            
            File.Delete(filepath);

            return Observable.Return(Unit.Default);
        }

        public IObservable<object> LoadState()
        {
            /* This LoadState() implementation has been adapted from an example provided in the 
             * ReactiveUI handbook. 
             * See: https://www.reactiveui.net/docs/handbook/data-persistence.html 
             * 
             * Unfortunately, the example code raises warning CS8619, identifying that the 
             * nullability of the returned object does not match that of the target return type. In 
             * this example code, an exception will be raised when the application tries to return 
             * a null object. 
             * 
             * As the ISuspensionDriver interface does not permit this method to return null 
             * objects, I can't see any way around this behaviour. Therefore, I have opted to 
             * explicitly throw a JsonSerializationException when this occurs. 
             * 
             * The ReactiveUI data persistence functionality is smart enough to handle this 
             * exception. When it occurs, a default application state will be retrieved by via 
             * CreateNewAppState(). */

            object? appState = DeserialiseFile<T>(filepath);

            return 
                appState == null ? 
                throw new JsonSerializationException() : 
                Observable.Return(appState);
        }

        public IObservable<Unit> SaveState(object state)
        {
            string jsonText = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(filepath, jsonText);

            return Observable.Return(Unit.Default);
        }
    }
}
