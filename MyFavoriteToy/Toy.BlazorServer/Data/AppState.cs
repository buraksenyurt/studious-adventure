using System;

namespace Toy.BlazorServer.Data
{
    public class AppState
    {
        public ToyModel CurrentToy { get; private set; }
        public event Action OnChange;

        public void SetAppState(ToyModel toy)
        {
            CurrentToy = toy;
            NotifyStateChanged();
        }

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
