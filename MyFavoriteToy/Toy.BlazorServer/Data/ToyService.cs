using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Toy.BlazorServer.Data
{
    public class ToyService
    {
        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection;
        public int NewToyId { get; set; }
        public string NewToyNickName { get; set; }
        public event Action OnChange;
        public ToyService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ToyModel>> GetTopFiveAsync()
        {
            var response = await _httpClient.GetAsync("/api/toy/topfive");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<IEnumerable<ToyModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return data;
        }

        public async Task UpdateAsync(ToyModel toy)
        {
            var response = await _httpClient.PutAsJsonAsync("/api/toy", toy);
            response.EnsureSuccessStatusCode();
        }

        public async Task InitSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_httpClient.BaseAddress.AbsoluteUri}ToyApiHub")
                .Build();

            _hubConnection.On<int, string>("NotifyNewToyAdded", (id, nickName) =>
            {
                NewToyId = id;
                NewToyNickName = nickName;
                NotifyStateChanged();
            });

            await _hubConnection.StartAsync();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
