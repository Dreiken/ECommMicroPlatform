# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY . .
RUN dotnet restore

# Publish the application
RUN dotnet publish -c Release -o /app

# Use the official .NET runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app .

# Expose the port the app runs on
EXPOSE 80

# Set the ASPNETCORE_URLS environment variable
ENV ASPNETCORE_URLS=http://+:80

RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Start the application
ENTRYPOINT ["dotnet", "OrderService.dll"]