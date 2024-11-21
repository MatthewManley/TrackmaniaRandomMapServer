#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
# having this enabled breaks the ability to write to the volume, technically decreases security
# more info on why this should be enabled can be found here: https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/
#USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NadeoAPI/NadeoAPI.csproj", "NadeoAPI/"]
COPY ["TmxRestClient/TrackmaniaExchangeAPI.csproj", "TmxRestClient/"]
COPY ["TrackmaniaRandomMapServer/TrackmaniaRandomMapServer.csproj", "TrackmaniaRandomMapServer/"]
RUN dotnet restore "./TrackmaniaRandomMapServer/TrackmaniaRandomMapServer.csproj"
COPY . .
WORKDIR "/src/TrackmaniaRandomMapServer"
RUN dotnet build "./TrackmaniaRandomMapServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TrackmaniaRandomMapServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrackmaniaRandomMapServer.dll"]