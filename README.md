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

## 4 - Model ve DbContext sınıflarının oluşturulması

