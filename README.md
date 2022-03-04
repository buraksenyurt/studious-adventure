# studious-adventure

Doğuş Teknoloji Geleceğe Giriş programı kapsamında hazırladığım Blazor'a giriş eğitimine ait notları içeren çalışmadır. Windows tabanlı bir sistemde en sevdiğimiz oyuncakları fotoğrafları ile birlikte saklayabileceğimiz bir çözüm için adım adım ilerlenmektedir.

## 0 - Solution ve Web API projesinin oluşturulması

```bash
dotnet new sln -o MyFavoriteToy
cd .\MyFavroiteToy\
dotnet new webapi -o ToyApi
dotnet sln add .\ToyApi\

# delete WeatherForecast and WeatherForecastController types
```

## 1 - PostgreSQL için Docker Hazırlıkları

WebAPI projesine aşağıdaki yaml dosyası eklenir.

```yml
version: '3.4'
 
services:
 
  postgresql_database:
    image: postgres:latest
    environment:
      - POSTGRES_USER=scoth
      - POSTGRES_PASSWORD=tiger
      - POSTGRES_DB=MyFavoriteToyDb
    ports:
      - "5432:5432"
    restart: always
    volumes:
      - database-data:/var/lib/postgresql/data/
     
  pgadmin:
    image: dpage/pgadmin4
    environment:
      - PGADMIN_DEFAULT_EMAIL=scoth@tiger.com
      - PGADMIN_DEFAULT_PASSWORD=tiger
    ports:
      - "5050:80"
    restart: always
    volumes:
      - pgadmin:/root/.pgadmin
 
volumes:
  database-data:
  pgadmin:
```

Terminalden aşağıdaki komut çalıştırılır.

```bash
docker-compose up -d
```

Kontrol için http://localhost:5050/login adresine gidilir. (Login kullanıcı adı: scoth@tiger.com, şifre: tiger)

## 2 - PostgreSQL Sunucu Hazırlıkları

pgAdmin arabirimindeyken Create->Server'dan yeni bir sunucu tanımı eklenir. 

```text
General Name            :   toyserver
Connection Hostname     :   postgresql_database
Port                    :   5432
Maintanance database    :   MyFavoriteToyDb
Username                :   scoth
Password                :   tiger
```

## 3 - Web API Projesine gerekli NuGet paketlerinin eklenmesi

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 5.0.5.1
dotnet add package Microsoft.EntityFrameworkCore --version 5.0.5
dotnet add package Microsoft.EntityFrameworkCore.Design --version 5.0.5
dotnet tool install --global dotnet-ef
```

## 4 - Model ve DbContext sınıflarının oluşturulması ile Migration işlemleri

WebAPI projesinin Root dizininde Db isimli bir klasör oluştur. 
Db klasörü altında altında Models isimli bir klasör oluştur.

Models altına Toy isimli sınıfı ekle.

```csharp
using System;

namespace ToyApi.Db.Models
{
    public class Toy
    {
        public int ToyId { get; set; }
        public string Nickname { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Like { get; set; }
        public string Photo { get; set; }
    }
}
```

Db klasöründe MyFavoriteToyDbContext isimli sınıfı oluştur.

```csharp
using Microsoft.EntityFrameworkCore;
using System;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public class MyFavoriteToyDbContext
        : DbContext
    {
        public MyFavoriteToyDbContext(DbContextOptions<MyFavoriteToyDbContext> options) : base(options)
        {
        }

        public DbSet<Toy> Toys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Toy>().HasData(new Toy
            {
                ToyId = 1,
                Nickname = "Kırmızı Şimşek",
                Description = "En sevdiğim oyuncak arabamdır :)",
                LastUpdated = DateTime.Now.AddDays(-1),
                Photo = PhotoUtility.GetBase64("kirmizi_simsek.jpg", "image/jpeg")
            });

            modelBuilder.Entity<Toy>().HasData(new Toy
            {
                ToyId =2,
                Nickname = "Çekici Meytır",
                Description = "Oğlumla severek izlediğim animasyonlardan Cars'ın eğlenceli karakteri Meytır.",
                LastUpdated = DateTime.Now,
                Photo = PhotoUtility.GetBase64("cekici_meytir.jpg", "image/jpeg")
            });
        }
    }
}
```

Oyuncak fotoğrafını base64 string olarak almak için PhotoUtility isimli bir sınıf oluştur.

```csharp
using System;
using System.IO;

namespace ToyApi
{
    public class PhotoUtility
    {
        public static string GetBase64(string fileName,string fileType)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Db/Images", fileName);
            var bytes = File.ReadAllBytes(path);
            return $"data:{fileType};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
```

WebAPI projesinin appSettings.json dosyasına bağlantı bilgisini ekle.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "DevConStr": "Host=localhost;Port=5432;Database=MyFavoriteToyDb;Username=scoth;Password=tiger"
  },
  "AllowedHosts": "*"
}
```

Startup içerisindeki ConfigureServices'da DI Servis bildirimini yap.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<MyFavoriteToyDbContext>(options =>
    {
        options.UseNpgsql(Configuration.GetConnectionString("DevConStr"));
    });
```

Terminalden aşağıdaki komutlarını kullanarak migration işlemini gerçekleştir.

```bash
dotnet ef migrations add InitialMigration
dotnet ef database update
```

## 5 - SignalR Entegrasyonu

WebAPI projesinin root klasörüne ToyApiHub isimli sınıfı ekle.

```csharp
using Microsoft.AspNetCore.SignalR;

namespace ToyApi
{
    public class ToyApiHub
        :Hub
    {
    }
}
```

SignalR ve ResponseCompression servislerini DI'a ekle. (ConfigureServices metodu)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSignalR();
    services.AddResponseCompression(options =>
    {
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
    });
    services.AddDbContext<MyFavoriteToyDbContext>(options =>
    {
        options.UseNpgsql(Configuration.GetConnectionString("DevConStr"));
    });
```

Startup sınıfındaki Configure metodunda Hub için Route bildirimini ekle.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ToyApiHub>("/ToyApiHub");
});
```

## 6 - Repository tiplerinin eklenmesi

Db klasörü altına IToyRepository ve ToyRepository sınıflarını ekle.

IToyRepository.cs

```csharp
using System.Collections.Generic;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public interface IToyRepository
    {
        IEnumerable<Toy> GetTopFive();
        int Create(Toy toy);
        Toy Update(Toy toy);
    }
}
```

ToyRepository.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public class ToyRepository
        : IToyRepository
    {
        private readonly MyFavoriteToyDbContext _dbContext;
        public ToyRepository(MyFavoriteToyDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public int Create(Toy toy)
        {
            var newId = _dbContext.Toys.Select(t => t.ToyId).Max() + 1;
            toy.ToyId = newId;
            toy.LastUpdated = DateTime.Now;

            _dbContext.Toys.Add(toy);
            var inserted=_dbContext.SaveChanges();
            return inserted;
        }

        public IEnumerable<Toy> GetTopFive()
        {
            var result = _dbContext.Toys.OrderBy(t => t.Like).Take(5);
            return result;
        }

        public Toy Update(Toy toy)
        {
            var current = _dbContext.Toys.FirstOrDefault(t => t.ToyId == toy.ToyId);
            if(current!=null)
            {
                current.LastUpdated = toy.LastUpdated;
                current.Like = toy.Like;
                current.Nickname = toy.Nickname;
                current.Description = toy.Description;
                current.Photo = toy.Photo;

                _dbContext.SaveChanges();

                return current;
            }
            return null;
        }
    }
}
```

IToyRepository bağımlılığı için ConfigureServices metoduna DI bildirimini ekle.

```csharp
services.AddScoped<IToyRepository, ToyRepository>();
```

## 7 - Controller Sınıfının Eklenmesi

Controller klasörüne ToyController sınıfını ekle.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ToyApi.Db;
using ToyApi.Db.Models;

namespace ToyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToyController : ControllerBase
    {
        private readonly IToyRepository _toyRepository;
        private readonly IHubContext<ToyApiHub> _hubContext;

        public ToyController(IToyRepository toyRepository, IHubContext<ToyApiHub> hubContext)
        {
            _toyRepository = toyRepository;
            _hubContext = hubContext;
        }

        [HttpGet()]
        [Route("TopFive")]
        public IActionResult GetTopFive()
        {
            var topFive = _toyRepository.GetTopFive();
            return Ok(topFive);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Toy toy)
        {
            var inserted = _toyRepository.Create(toy);
            if (inserted > 0)
            {
                _hubContext.Clients.All.SendAsync("NotifyNewToyAdded", toy.ToyId, toy.Nickname);
                return Ok("New toy has been added successfully.");
            }

            return BadRequest();
        }

        [HttpPut]
        public IActionResult Update([FromBody] Toy toy)
        {
            var updated = _toyRepository.Update(toy);
            if (updated != null)
            {
                return Ok("Toy has been updated successfully.");
            }
            else
            {
                return NotFound();
            }
        }
    }
}
```

## 8 - WebAPI servisinin çalıştırılıp denenmesi

Örneği çalıştır https://localhost:5001/swagger/index.html Swagger adresinden metotlarını dene.

Örnek Get;
```text`
https://localhost:5001/api/Toy/TopFive
```

Örnek Create;

```json
{
  "nickname": "Obi Wan Ruhu",
  "description": "Obi wan'ın gemisini yaptığım lego",
  "like": 1
}
```

Örnek Update;

```json
{
  "toyId": 3,
  "nickname": "Obi Wan's Spirit",
  "description": "Obi wan'ın gemisinin Legosu. Bir öğleden sonramı harika geçirmemi sağlamıştı.",
  "lastUpdated": "2021-05-08T18:22:18.104Z",
  "like": 9
}
```

## 9 - Farklı Domain'lerin WebAPI'yi Kullanabilmesi için CORS Eklenmesi

CORS: Cross-Origin Resource Sharing

ConfigureServices metoduna aşağıdaki kısmı ekle.

```csharp
services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
```

Ayrıca Configure metodunda aşağıdaki satırı ekleyerek CORS middleware'ini etkinleştir.

```csharp
app.UseCors("AllowAll");
```

## 10 - Blazor Server Projesinin Oluşturulması

```bash
dotnet new blazorserver -o Toy.BlazorServer
dotnet sln add .\Toy.BlazorServer\
```

## 11 - Blazor Server projesine model sınıfının eklenmesi

Data klasörüne ToyModel sınıfını ekle.

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace Toy.BlazorServer.Data
{
    public class ToyModel
    {
        public int ToyId { get; set; }
        [Required]
        [MinLength(10,ErrorMessage ="Yaratıcı düşün. Güzel bir oyuncak adı ver")]
        [MaxLength(30,ErrorMessage ="O kadar da uzun bir isim olmasın")]
        public string Nickname { get; set; }
        [Required]
        [MinLength(20, ErrorMessage = "Yaratıcı düşün. Onun hakkında daha fazla şey söyle")]
        [MaxLength(250, ErrorMessage = "O kadar da uzun bir açıklama olmasın")]
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Like { get; set; }
        public string Photo { get; set; }
    }
}
```

## 12 - Blazor Server uygulamasında Web API iletişimi için servis entegrasyonunun yapılması.

SignalR iletişimi için gerekli paketi ekle.

```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client -v 5.0.5
```

Data klasörüne ToyService sınıfını ekle.

```csharp
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
```

appSetting.json dosyasına WebApi base address için tanım ekle.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ToyApiBaseUrl": "https://localhost:44374/",
  "AllowedHosts": "*"
}
```

DI container servisine HttpClient Servisini kaydet. (ConfigureServices metodu)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient<ToyService>(client =>
    {
        client.BaseAddress = new Uri(Configuration["ToyApiBaseUrl"]);
    });
    services.AddRazorPages();
    services.AddServerSideBlazor();
}
```

## 13 - Blazor Server uygulamasındaki bileşenlerin değişiklikleri takip etmesi için State sınıfının eklenmesi

Data klasörüne AppState isimli sınıfı ekle.

```csharp
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
```

AppState sınıfını DI Container servislerine kayıt et.(ConfigureServices metodu)

```csharp
services.AddScoped<AppState>();
```

## 14 - Blazor Server uygulamasına temel Razor Component'lerinin eklenmesi

Pages klasörüne EditToy.razor ve ViewToy.razor bileşenlerini ekle. 

```csharp
@using Toy.BlazorServer.Data
@inject ToyService _toyService
@inject AppState _appState

@if(IsPreviewMode)
{
    <ViewToy ToyModel="ToyModel"/>
}
else
{
    <EditForm Model="@ToyModel" OnValidSubmit="HandleValidSubmit">
        <div class="card-body">
            <DataAnnotationsValidator />
            Nickname :
            <InputText class="form-control" @bind-Value="ToyModel.Nickname" />
            <ValidationMessage For="@(()=>ToyModel.Nickname)" />
            Description :
            <InputTextArea class="form-control" @bind-Value="ToyModel.Description" />
            <ValidationMessage For="@(()=>ToyModel.Description)" />
            <br />
            <button type="submit" class="btn btn-outline-primary">Kaydet</button>
            <button type="button" class="btn btn-outline-dark" @onclick="HandleUndoChanges">Geri Al</button>
        </div>
    </EditForm>
}
```

EditToy.razor'ın kod tarının ayrı bir dosyada anlatmak için EditToy.cs isimli partial sınıfı ekle.

```csharp
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
```

ViewToy.razor bileşeni C# kodlarını üstünde taşıyor. Aşağıdaki gibi ekle.

```csharp
@using Toy.BlazorServer.Data
@inject ToyService _toyService
@inject AppState _appState

@if (IsEditMode)
{
    <EditToy ToyModel="ToyModel" />
}
else
{
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">@ToyModel.Nickname</h4>
            <p class="card-text">
                <i>@ToyModel.Description</i>
            </p>
            <p>
                <b>@ToyModel.Like</b> defa beğenildi.
                <br />
                Son güncelleme @ToyModel.LastUpdated.ToShortDateString()
            </p>
            <button type="button" class="btn btn-outline-primary" @onclick="(()=>IsEditMode=true)">Düzenle</button>
            <button type="button" class="btn btn-outline-primary" @onclick="(()=>GiveALike(ToyModel.ToyId))">Beğendim</button>
        </div>
        <img class="card-img-bottom" src="@ToyModel.Photo" />
    </div>
}

@code{
    [Parameter]
    public ToyModel ToyModel { get; set; }
    bool IsEditMode { get; set; } = false;

    private async void GiveALike(int toyId)
    {
        ToyModel.Like += 1;
        await _toyService.UpdateAsync(ToyModel);
        _appState.SetAppState(ToyModel);
    }
}
```

## 15 - Blazor Server uygulamasında ana bileşeni tasarla.

Pages klasörü altına main.razor bileşenini ekle ve aşağıdaki gibi kodla.

```csharp
@page "/main"
@using Toy.BlazorServer.Data
@inject ToyService _toyService
@inject AppState _appState
@implements IDisposable

@if (Toys != null)
{
    <div class="container">
        <div class="row">
            <div class="col-8">
                <h3>Öne Çıkan Oyuncak</h3>
                <ViewToy ToyModel="ToyModel" />
            </div>
            <div class="col-4">
                <div class="row">
                    <h3>Popüler 5</h3>
                    <div class="card">
                        <div class="card-body">
                            <ul>
                                @foreach (var toy in Toys)
                                {
                                    <li>
                                        <a href="javascript:void(0)" @onclick="(()=>ShowDetails(toy.ToyId))">@toy.Nickname</a>
                                    </li>
                                }
                            </ul>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <h3>Haberler</h3>
                    <div class="card">
                        <div class="card-body">
                            <h5 class="card-title">@_toyService.NewToyNickName</h5>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}
else
{
    <p><em>Heyecanlı bir bekleyiş...</em></p>
}

@code{
    private IEnumerable<ToyModel> Toys;
    public ToyModel ToyModel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _toyService.InitSignalR();
        Toys = await _toyService.GetTopFiveAsync();
        ToyModel = Toys.FirstOrDefault();

        _toyService.NewToyNickName = ToyModel.Nickname;
        _toyService.NewToyId = ToyModel.ToyId;
        _toyService.OnChange += NewToyAdded;
        _appState.OnChange += StateChanged;
    }

    private async void NewToyAdded()
    {
        Toys = await _toyService.GetTopFiveAsync();
        StateHasChanged();
    }

    private async void StateChanged()
    {
        Toys = await _toyService.GetTopFiveAsync();
        ToyModel = _appState.CurrentToy;
        if (_toyService.NewToyId == _appState.CurrentToy.ToyId)
            _toyService.NewToyNickName = _appState.CurrentToy.Nickname;

        StateHasChanged();
    }

    public void Dispose()
    {
        _appState.OnChange -= StateHasChanged;
        _toyService.OnChange -= StateHasChanged;
    }

    private void ShowDetails(int toyId)
    {
        ToyModel = Toys.FirstOrDefault(t => t.ToyId == toyId);
    }
} 
```

NavMenu bileşenini güncelle.

```html
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="main" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span>Oyuncak Şehri
            </NavLink>
        </li>
    </ul>
</div>
```

Uygulamanın bu noktada aşağıdakine benzer şekilde çalıştığını teyit et.

![assets/asset_01.png](assets/asset_01.png)

Kontroller _(Debug ederek ilerle)_

- Oyuncak Şehri linkine tıklandığında oyuncaklar gelmeli.
- Sağ tarafda en popüler oyuncaklar çıkmalı.
- Bir oyuncağın başlık bilgisi değiştiğinde sağ taraftaki Popüler 5 ve Haberler kısımlarında da değişiklik olmalı.
- Beğendim butonuna basıldığında beğenme sayısı artmalı.
- Düzenle ile birkaç bilgi düzenlenip sonrasında Geri Al tuşuna basıldığında oyuncak bilgileri eski halinde kalmalı.

## 16 - Blazor Web Assembly Projesinin oluşturulması

```bash
dotnet new blazorwasm -o Toy.BlazorWasm
dotnet sln add .\Toy.BlazorWasm\
```

Template üstünden hazır gelen dosyaları sil. sample-data klasörü, Counter.razor, FetchData.Razor, SurveyPrompt.razor bileşenleri. NavMenu.razor'dan silinen bileşenlere ait sayfa linklerini kaldır.

## 17 - Blazor WASM projesinde DTO(Data Transform Object) oluşturulması

root klasörde NewToyRequest isimli aşağıdaki sınıfı oluştur.

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace Toy.BlazorWasm
{
    public class NewToyRequest
    {
        public int ToyId { get; set; }
        [Required]
        [MinLength(10, ErrorMessage = "Yaratıcı düşün. Güzel bir oyuncak adı ver")]
        [MaxLength(30, ErrorMessage = "O kadar da uzun bir isim olmasın")]
        public string Nickname { get; set; }
        [Required]
        [MinLength(20, ErrorMessage = "Yaratıcı düşün. Onun hakkında daha fazla şey söyle")]
        [MaxLength(250, ErrorMessage = "O kadar da uzun bir açıklama olmasın")]
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Like { get; set; }
        public string Photo { get; set; }
    }
}
```

## 18 - Blazor WASM projesindeki Index bileşeninin yazılması

```csharp
@page "/"
@inject HttpClient _httpClient

@code{
    NewToyRequest ToyModel = new NewToyRequest();
    string photoData;
    string statusMessage;
    string errorMessage;
    public async Task HandleValidSubmit()
    {
        ToyModel.Photo = photoData;
        ToyModel.Like = 1;
        await _httpClient.PostAsJsonAsync("https://localhost:44374/api/toy", ToyModel);
    }
    public async Task HandleFileSelection(InputFileChangeEventArgs args)
    {
        errorMessage = string.Empty;
        var file = args.File;
        if (file != null)
        {
            if (file.Size > (5 * 1024 * 1024))
            {
                errorMessage = "Bu dosya kapasitemin çok üstünde. En fazla 5 Mb dosya kabul edebilirim :(";
            }

            if (file.ContentType == "image/jpg" || file.ContentType == "image/jpeg" || file.ContentType == "image/png")
            {
                var buffer = new byte[file.Size];
                await file.OpenReadStream().ReadAsync(buffer);

                photoData = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";
                statusMessage = "Oyuncak fotoğrafın yüklendi. Yii haa :)";
            }
            else
            {
                errorMessage = "Üzgünüm ama sadece jpg/jpeg/png uzantılı dosyalarla çalışıyorum.";
                return;
            }
        }

    }
}

<h1>Sevdiğim Bir Oyuncağımı Eklemek İstiyorum</h1>
<EditForm Model="@ToyModel" OnValidSubmit="HandleValidSubmit">
    <div class="card">
        <div class="card-body">
            <DataAnnotationsValidator />
            @*<ValidationSummary />*@
            Fotoğraf Seç:
            <InputFile OnChange="HandleFileSelection" />
            <p class="alert-danger">@errorMessage</p>
            <p class="alert-info">@statusMessage</p>
            <p>
                <img src="@photoData" style="width:250px;height:250px" />
            </p>
            Takma Adı:
            <InputText class="form-control" id="nickName" @bind-Value="ToyModel.Nickname" />
            <ValidationMessage For="@(()=>ToyModel.Nickname)" />
            <InputTextArea class="form-control" id="description" @bind-Value="ToyModel.Description" />
            <ValidationMessage For="@(()=>ToyModel.Description)" />
            <br />
            <button type="submit" class="btn btn-outline-primary oi-align-center">Gönder</button>
        </div>
    </div>
</EditForm>
```

## 19 - Komple Test

Solution'daki üç projeyi de aynı anda başlayacak şekilde ayarla. _(Solution -> Properties -> Multiple startup projects)_

- Blazor WASM uygulamasından fotoğraf ile birlikte yeni bir oyuncak yükle. Yüklenen oyuncak bilgileri Blazor Server uygulamasını güncellemeye gerek kalmadan oradaki ekrana yansımalı.
- Beğendim tuşlarını kullanarak oyuncakların sıralamasının anlık olarak değişip değişmediğini gözlemle.
- 5 Mb'tan büyük fotoğraf yüklemeye çalış.
- Web API projesini durdurup yeni bir oyuncak eklemeye çalış.

![assets/asset_02.png](assets/asset_02.png)

## 20 - Blazor Web Assembly PWA(Progressive Web App) Versiyonunun Eklenmesi

```bash
dotnet new blazorwasm -o Toy.BlazorWasmPWA --pwa
dotnet sln add .\Toy.BlazorWasmPWA\
```

## 21 - Blazor Web Assembly PWA'ya Diğer Uygulamadaki Dosyaların Alınması

- NewToyRequest dosyası taşınır, namespace değiştirilir.
- Index.razor'un içi aynen taşınır.
- NavMenu.razor eşitlenir.

Blazor Server, Web API ve PWA uygulamaları başlayacak şekilde Solution ayarlanır. PWA tarayıcıda açıldığında sağ üstten Install seçeneği ile yüklenmesi sağlanır. Aşağıdaki gibi çalışıyor olması gerekir.

![assets/asset_03.png](assets/asset_03.png)

_Not: Offline çalışma desteği için öncelikle PWA uygulamasını host edileceği bir ortama Publish edilmesi gerekir._

Kaynaklar : [ASP.NET Core 5 for Beginners](https://www.packtpub.com/product/asp-net-core-5-for-beginners/9781800567184?utm_source=github&utm_medium=repository&utm_campaign=9781800567184), Andreas Helland, Vincent Maverick Durano, Jeffrey Chilberto, Ed Price

## Ek - Ubuntu

Üstünden epey zaman geçen bu eğitimi geçenlerde tekrar verdim. Windows bilgisayarımda problem çıktığı içinde Ubuntu üstüne almam gerekti. Ubuntu tarafında sadece .Net 6 yüklemişim. Bu sebepten proje dosyalarındaki 

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>
```

kısımlarını

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
```

olarak değiştirdim. 

Bazı paketlerin 5.x sürümleri bu versiyonda sorun çıkardığı için 6.x sürümlerine çekildi. Ubuntu tarafında IIS ve Visual Studio olmadığından multiple startup project teorisi çöktü ve CORS hataları ile başbaşa kaldım. Bunun üzerine _dotnet run_ ile projeleri ayağa kaldırırken, çağrı yapılan ve kullanılan port adreslerinde değişikliğe gittim. Ubuntu tarafında örneği test etmek isteyenler _dotnet run_ ile ilerleyebilirler.

```bash
# Web API
# ToyApi altındayken
# http://localhost:44374 adresinden ayağa kalkar.
dotnet run

# Server Side Blazor
# Toy.BlazorServer klasöründeyken
# https://localhost:5001/ adresinden ayağa kalkar.
dotnet run

# Client Side Blazor
# Toy.BlazorWasm klasöründeyken
# https://localhost:6001 adresinden ayağa kalkar.
dotnet run 

# PWA Blazor
# Toy.BlazorWasmPWA klasöründeyken
# https://localhost:7001 adresinden ayağa kalkar.
dotnet run
```