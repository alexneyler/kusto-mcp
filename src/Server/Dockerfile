# Use the official .NET 9.0 runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Use the official .NET 9.0 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . ./

# Restore dependencies
RUN dotnet restore Server/Server.csproj

# Build the application
RUN dotnet publish Server/Server.csproj -c Release -o /app/publish

# Final stage: runtime image
FROM base AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "Server.dll", "--settings", "./settings.yaml"]
