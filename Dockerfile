FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy everything else and build
COPY . ./
COPY ./ConsoleAppStarter/appsettings.json.docker.prod ./ConsoleAppStarter/appsettings.json
RUN mkdir -p /app/data/dbs && mkdir -p /app/data/source && mkdir /app/data/target
WORKDIR /app/ConsoleAppStarter
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:3.1
#COPY bin/Release/netcoreapp3.1/ app/
WORKDIR /app
COPY --from=build-env /app/ConsoleAppStarter/out .
COPY --from=build-env /app/MediaArchiverTests/assets ./test
ENTRYPOINT ["dotnet", "ConsoleAppStarter.dll"]