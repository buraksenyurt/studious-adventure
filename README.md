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
app.UseCors();
```