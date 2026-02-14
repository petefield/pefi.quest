FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/AdventureGame.Shared/AdventureGame.Shared.csproj src/AdventureGame.Shared/
COPY src/AdventureGame.Client/AdventureGame.Client.csproj src/AdventureGame.Client/
RUN dotnet restore src/AdventureGame.Client/AdventureGame.Client.csproj

# Copy source and publish
COPY src/AdventureGame.Shared/ src/AdventureGame.Shared/
COPY src/AdventureGame.Client/ src/AdventureGame.Client/
RUN dotnet publish src/AdventureGame.Client/AdventureGame.Client.csproj -c Release -o /app/publish

FROM nginx:alpine AS runtime
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/AdventureGame.Client/nginx.conf /etc/nginx/conf.d/default.conf

# Override appsettings so ApiBaseUrl is empty (same-origin via nginx proxy)
RUN echo '{}' > /usr/share/nginx/html/appsettings.json

EXPOSE 80
