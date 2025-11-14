# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0.415-azurelinux3.0 AS build

# Arguments
ARG BUILD_CONFIGURATION=Release

# Workdir
WORKDIR /src

# Copy statements
COPY ["src/MercuriusAPI", "MercuriusAPI/"]

# Run statements
RUN dotnet restore \
    "./MercuriusAPI/MercuriusAPI.csproj"

RUN dotnet build \
    "./MercuriusAPI/MercuriusAPI.csproj" \
    --no-restore  \
    --configuration $BUILD_CONFIGURATION \
    --output /app/build

RUN dotnet publish \
    "./MercuriusAPI/MercuriusAPI.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0.21-azurelinux3.0 AS run
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MercuriusAPI.dll"]