using DateToday.ViewModels;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace DateToday.Drivers
{
    public class SuspensionDriver(string filePath) : ISuspensionDriver
    {
        private readonly string _stateFilePath = filePath;
        private readonly JsonSerializerSettings _settings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public IObservable<Unit> InvalidateState()
        {
            if (File.Exists(_stateFilePath))
                File.Delete(_stateFilePath);
            return Observable.Return(Unit.Default);
        }

        public IObservable<object> LoadState()
        {
            if (!File.Exists(_stateFilePath))
            {
                File.WriteAllText(_stateFilePath, "null");
            }

            var lines = File.ReadAllText(_stateFilePath);
            WidgetViewModel state = 
                JsonConvert.DeserializeObject<WidgetViewModel>(lines, _settings);

            return Observable.Return(state);
        }

        public IObservable<Unit> SaveState(object state)
        {
            var lines = JsonConvert.SerializeObject(state, _settings);
            File.WriteAllText(_stateFilePath, lines);
            return Observable.Return(Unit.Default);
        }
    }
}
