FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a csproj changes
COPY global.json Directory.Build.props ./
COPY src/UserService.Api/UserService.Api.csproj src/UserService.Api/
COPY src/UserService.Service/UserService.Service.csproj src/UserService.Service/
COPY src/UserService.Repository/UserService.Repository.csproj src/UserService.Repository/
COPY src/UserService.Domain/UserService.Domain.csproj src/UserService.Domain/
RUN dotnet restore src/UserService.Api/UserService.Api.csproj

COPY src/ src/
RUN dotnet publish src/UserService.Api/UserService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.Api.dll"]
