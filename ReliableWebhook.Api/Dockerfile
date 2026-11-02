FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ReliableWebhook.Api/ReliableWebhook.Api.csproj ReliableWebhook.Api/
COPY ReliableWebhook.Infrastructure/ReliableWebhook.Infrastructure.csproj ReliableWebhook.Infrastructure/

RUN dotnet restore ReliableWebhook.Api/ReliableWebhook.Api.csproj

COPY . .

RUN dotnet publish ReliableWebhook.Api/ReliableWebhook.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ReliableWebhook.Api.dll"]