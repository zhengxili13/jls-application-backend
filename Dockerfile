# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["JLSMobileApplication/JLSApplicationBackend.csproj", "JLSMobileApplication/"]
COPY ["JLSDataModel/JLSDataModel.csproj", "JLSDataModel/"]
COPY ["JLSDataAccess/JLSDataAccess.csproj", "JLSDataAccess/"]

RUN dotnet restore "JLSMobileApplication/JLSApplicationBackend.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/JLSMobileApplication"

# Build the application
RUN dotnet build "JLSApplicationBackend.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "JLSApplicationBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create the Exports directory
RUN mkdir -p /app/Exports && chown -R dotnet:dotnet /app/Exports

# Set Environment Variables
ENV ASPNETCORE_URLS=http://+:80
ENV ExportPath=/app/Exports

EXPOSE 80

# Run as non-root for security
USER $APP_UID

ENTRYPOINT ["dotnet", "JLSApplicationBackend.dll"]
