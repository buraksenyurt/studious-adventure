using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Toy.BlazorServer.Data;

namespace Toy.BlazorServer.Pages
{
    public partial class EditToy
    {
        [Parameter]
        public ToyModel ToyModel { get; set; }
        private ToyModel ToyBackup { get; set; }
        public bool IsPreviewMode { get; set; } = false;

        protected override void OnInitialized()
        {
            ToyBackup = new ToyModel
            {
                ToyId= ToyModel.ToyId,
                Nickname= ToyModel.Nickname,
                Description= ToyModel.Description,
                Photo= ToyModel.Photo,
                Like= ToyModel.Like,
                LastUpdated= ToyModel.LastUpdated
            };
        }

        protected async Task HandleValidSubmit()
        {
            await _toyService.UpdateAsync(ToyModel);
            IsPreviewMode = true;
            _appState.SetAppState(ToyModel);
        }

        protected void HandleUndoChanges()
        {
            IsPreviewMode = true;
            if(ToyModel.Nickname.Trim()!=ToyBackup.Nickname.Trim()
                || ToyModel.Description.Trim()!=ToyBackup.Description.Trim())
            {
                ToyModel = ToyBackup;
                _appState.SetAppState(ToyBackup);
            }
        }
    }
}
