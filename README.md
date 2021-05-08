# studious-adventure

It is educational material with simple steps to learn Blazor. It is designed to be used in Doğuş Teknoloji internal training.

# Steps

## 1 - Create Solution and WebApi

```bash
dotnet new sln -o MyFavoriteToy
cd .\MyFavroiteToy\
dotnet new webapi -o ToyApi
dotnet sln add .\ToyApi\

# delete WeatherForecast and WeatherForecastController types
```