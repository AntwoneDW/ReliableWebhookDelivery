FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ReliableWebhook.Worker/ReliableWebhook.Worker.csproj ReliableWebhook.Worker/
COPY ReliableWebhook.Infrastructure/ReliableWebhook.Infrastructure.csproj ReliableWebhook.Infrastructure/

RUN dotnet restore ReliableWebhook.Worker/ReliableWebhook.Worker.csproj

COPY . .

RUN dotnet publish ReliableWebhook.Worker/ReliableWebhook.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReliableWebhook.Worker.dll"]