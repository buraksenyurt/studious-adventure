using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Toy.BlazorServer.Data;

namespace Toy.BlazorServer.Pages
{
    public partial class EditToy
    {
        [Parameter]
        public ToyModel Toy { get; set; }
        private ToyModel ToyBackup { get; set; }
        public bool IsPreviewMode { get; set; } = false;

        protected override void OnInitialized()
        {
            ToyBackup = new ToyModel
            {
                ToyId=Toy.ToyId,
                Nickname=Toy.Nickname,
                Description=Toy.Description,
                Photo=Toy.Photo,
                Like=Toy.Like,
                LastUpdated=Toy.LastUpdated
            };
        }

        protected async Task HandleValidSubmit()
        {
            await _toyService.UpdateAsync(Toy);
            IsPreviewMode = true;
            _appState.SetAppState(Toy);

        }

        protected void HandleUndoChanges()
        {
            IsPreviewMode = true;
            if(Toy.Nickname.Trim()!=ToyBackup.Nickname.Trim()
                ||Toy.Description.Trim()!=ToyBackup.Description.Trim())
            {
                Toy = ToyBackup;
                _appState.SetAppState(ToyBackup);
            }
        }
    }
}
