# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ARG GOOGLE_BOOKS_API_KEY
ENV GOOGLE_BOOKS_API_KEY=$GOOGLE_BOOKS_API_KEY

# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookHeaven.Server/BookHeaven.Server.csproj", "BookHeaven.Server/"]
COPY ["EpubManager/EpubManager.csproj", "EpubManager/"]
COPY ["BookHeaven.Domain/BookHeaven.Domain.csproj", "BookHeaven.Domain/"]
RUN dotnet restore "./BookHeaven.Server/BookHeaven.Server.csproj"
#RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
#RUN apt-get install -y nodejs
COPY . .
WORKDIR "/src/BookHeaven.Server"
#RUN npm install
RUN dotnet build "./BookHeaven.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookHeaven.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookHeaven.Server.dll"]