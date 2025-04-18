# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WMS.Backend.WebApi/WMS.Backend.WebApi.csproj", "WMS.Backend.WebApi/"]
COPY ["WMS.Backend.Application/WMS.Backend.Application.csproj", "WMS.Backend.Application/"]
COPY ["WMS.Backend.Domain/WMS.Backend.Domain.csproj", "WMS.Backend.Domain/"]
COPY ["WMS.Backend.Common/WMS.Backend.Common.csproj", "WMS.Backend.Common/"]
COPY ["WMS.Shared/WMS.Shared.csproj", "WMS.Shared/"]
COPY ["WMS.Backend.Infrastructure/WMS.Backend.Infrastructure.csproj", "WMS.Backend.Infrastructure/"]
COPY ["WMS.Backend.MessageBus/WMS.Backend.MessageBus.csproj", "WMS.Backend.MessageBus/"]
RUN dotnet restore "./WMS.Backend.WebApi/WMS.Backend.WebApi.csproj"
COPY . .
WORKDIR "/src/WMS.Backend.WebApi"
RUN dotnet build "./WMS.Backend.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WMS.Backend.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WMS.Backend.WebApi.dll"]