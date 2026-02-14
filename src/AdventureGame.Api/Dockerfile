FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/AdventureGame.Shared/AdventureGame.Shared.csproj src/AdventureGame.Shared/
COPY src/AdventureGame.Api/AdventureGame.Api.csproj src/AdventureGame.Api/
RUN dotnet restore src/AdventureGame.Api/AdventureGame.Api.csproj

# Copy source and publish
COPY src/AdventureGame.Shared/ src/AdventureGame.Shared/
COPY src/AdventureGame.Api/ src/AdventureGame.Api/
RUN dotnet publish src/AdventureGame.Api/AdventureGame.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AdventureGame.Api.dll"]
