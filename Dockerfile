FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Rollout.Api/Rollout.Api.csproj src/Rollout.Api/
COPY src/Rollout.Shared/Rollout.Shared.csproj src/Rollout.Shared/
COPY src/Rollout.Modules.Auth/Rollout.Modules.Auth.csproj src/Rollout.Modules.Auth/
COPY src/Rollout.Modules.Users/Rollout.Modules.Users.csproj src/Rollout.Modules.Users/
COPY src/Rollout.Modules.Events/Rollout.Modules.Events.csproj src/Rollout.Modules.Events/

RUN dotnet restore src/Rollout.Api/Rollout.Api.csproj

COPY . .

RUN dotnet publish src/Rollout.Api/Rollout.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Rollout.Api.dll"]