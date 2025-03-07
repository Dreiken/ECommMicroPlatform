FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/ProductService
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

EXPOSE 80
ENTRYPOINT ["dotnet", "ProductService.dll"]