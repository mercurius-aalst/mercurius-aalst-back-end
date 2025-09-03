# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-azurelinux3.0 AS build

# Arguments
ARG BUILD_CONFIGURATION=Release
ARG InitialUser__Password
ARG InitialUser__Role
ARG InitialUser__Username
ARG Jwt__Key
ARG ConnectionStrings__MercuriusDB

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0-azurelinux3.0-distroless AS run
WORKDIR /app
COPY --from=build /app/publish .
ENV InitialUser__Password=${InitialUser__Password}
ENV InitialUser__Role=${InitialUser__Role}
ENV InitialUser__Username=${InitialUser__Username}
ENV Jwt__Key=${Jwt__Key}
ENV ConnectionStrings__MercuriusDB=${ConnectionStrings__MercuriusDB}
ENTRYPOINT ["dotnet", "MercuriusAPI.dll"]