# Use the official ASP.NET Core SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["forfreand-api/forfreand-api.csproj", "forfreand-api/"]
RUN dotnet restore "forfreand-api/forfreand-api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/forfreand-api"
RUN dotnet build "forfreand-api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "forfreand-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "forfreand-api.dll"]
