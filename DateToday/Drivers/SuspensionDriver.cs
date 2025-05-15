using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace DateToday.Drivers
{
    internal sealed class SuspensionDriver(string filePath, Type stateType) : ISuspensionDriver
    {
        public IObservable<Unit> InvalidateState()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<object> LoadState()
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "null");
            }

            string jsonText = File.ReadAllText(filePath);
            object? state = JsonConvert.DeserializeObject(jsonText, stateType);

            return Observable.Return(state)!;
        }

        public IObservable<Unit> SaveState(object state)
        {
            string jsonText = JsonConvert.SerializeObject(state);
            File.WriteAllText(filePath, jsonText);

            return Observable.Return(Unit.Default);
        }
    }
}
