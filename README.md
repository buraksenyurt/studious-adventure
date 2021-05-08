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