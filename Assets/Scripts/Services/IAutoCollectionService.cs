using System;
using UniRx;

namespace SwiftRiver.Services
{
    public interface IAutoCollectionService
    {
        IObservable<Unit> CollectionTriggered { get; }

        void StartCollecting();
        void StopCollecting();
    }
}
