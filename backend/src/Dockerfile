FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
LABEL build.configuration=$BUILD_CONFIGURATION
ENV DOTNET_RUNNING_IN=$BUILD_CONFIGURATION
WORKDIR /src
COPY ["LogopedicBackend.csproj", "./"]
COPY Migrations ./Migrations
RUN dotnet restore "LogopedicBackend.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./LogopedicBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LogopedicBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LogopedicBackend.dll"]
