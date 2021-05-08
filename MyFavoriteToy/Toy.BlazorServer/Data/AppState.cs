using System;

namespace Toy.BlazorServer.Data
{
    public class AppState
    {
        public Toy CurrentToy { get; private set; }
        public event Action OnChange;

        public void SetAppState(Toy toy)
        {
            CurrentToy = toy;
            NotifyStateChanged();
        }

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
