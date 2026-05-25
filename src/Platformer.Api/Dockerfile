FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Platformer.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5001

RUN mkdir -p /app/data

COPY --from=build /app/publish .

EXPOSE 5001

ENTRYPOINT ["dotnet", "Platformer.Api.dll"]