using System;
using System.Collections.Generic;
using SwiftRiver.Models.DogBreeds;
using UniRx;

namespace SwiftRiver.DogBreeds
{
    public interface IDogBreedsView
    {
        IObservable<int> BreedSelected { get; }

        void Show();
        void Hide();
        void UpdateBreedList(List<BreedInfo> breeds);
        void ShowLoading();
        void HideLoading();
        void ShowBreedLoading(int index);
        void HideBreedLoading(int index);
        void HideAllBreedLoading();
        void ShowError(string message);
    }
}
