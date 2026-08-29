FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:bb32ba3ba3ea36e38572d9d8db76fa15f7cbf722f3f886e06bca6d528bd4fba8 AS build
WORKDIR /src
COPY API/API.csproj API/
COPY Library/Library.csproj Library/
RUN dotnet restore API/API.csproj
COPY . .
WORKDIR /src/API
RUN dotnet publish API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0@sha256:97b7c4ac794663ee19c56ca56fa458612a3d943e2c94fe76fdce00ecc64a8537
ARG SOURCE_REVISION=unknown
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    VEDASTRO_SOURCE_REVISION=$SOURCE_REVISION
EXPOSE 80
