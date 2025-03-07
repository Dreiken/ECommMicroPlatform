FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/NotificationService
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NotificationService.dll"]