# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
#EXPOSE 8081

ARG GOOGLE_BOOKS_API_KEY
ENV GOOGLE_BOOKS_API_KEY=$GOOGLE_BOOKS_API_KEY
ENV OPENAPI_GENERATE=false

RUN mkdir -p /app/data &&  \
    chown -R app:app /app/data

USER app
# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookHeaven.Server/BookHeaven.Server.csproj", "BookHeaven.Server/"]

RUN --mount=type=secret,id=NUGET_TOKEN \
    dotnet nuget add source "https://nuget.pkg.github.com/BookHeaven/index.json" \
        --name BookHeaven \
        --username heasheartfire \
        --password $(cat /run/secrets/NUGET_TOKEN) \
        --store-password-in-clear-text

COPY . .
WORKDIR "/src/BookHeaven.Server"
RUN dotnet build "./BookHeaven.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:OpenApiGenerateDocumentsOnBuild=${OPENAPI_GENERATE} /p:UseNuget=true

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookHeaven.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:OpenApiGenerateDocumentsOnBuild=${OPENAPI_GENERATE} /p:UseNuget=true

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookHeaven.Server.dll"]