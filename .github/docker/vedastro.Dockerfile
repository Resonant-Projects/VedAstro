FROM mcr.microsoft.com/dotnet/sdk:7.0@sha256:d32bd65cf5843f413e81f5d917057c82da99737cb1637e905a1a4bc2e7ec6c8d AS build
WORKDIR /src
COPY API/API.csproj API/
COPY Library/Library.csproj Library/
RUN dotnet restore API/API.csproj
COPY . .
WORKDIR /src/API
RUN dotnet publish API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated7.0@sha256:3d2656f207c34a7603d0c4434aa9ff17ce1af77d3b3a5308052557b58fe5d51c
ARG SOURCE_REVISION=unknown
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    VEDASTRO_SOURCE_REVISION=$SOURCE_REVISION
EXPOSE 80
