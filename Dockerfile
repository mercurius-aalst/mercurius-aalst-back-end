FROM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["global.json", "./"]
COPY ["src", "src/"]

RUN dotnet restore \
    "./src/MercuriusAPI/Mercurius.LAN.API.csproj"

RUN dotnet build \
    "./src/MercuriusAPI/Mercurius.LAN.API.csproj" \
    --no-restore  \
    --configuration $BUILD_CONFIGURATION \
    --output /app/build

RUN dotnet publish \
    "./src/MercuriusAPI/Mercurius.LAN.API.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0 AS run
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Mercurius.LAN.API.dll"]
