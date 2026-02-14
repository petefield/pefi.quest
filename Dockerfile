# ---- Stage 1: Build the API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build-api
WORKDIR /src
COPY src/AdventureGame.Shared/AdventureGame.Shared.csproj src/AdventureGame.Shared/
COPY src/AdventureGame.Api/AdventureGame.Api.csproj src/AdventureGame.Api/
RUN dotnet restore src/AdventureGame.Api/AdventureGame.Api.csproj
COPY src/AdventureGame.Shared/ src/AdventureGame.Shared/
COPY src/AdventureGame.Api/ src/AdventureGame.Api/
RUN dotnet publish src/AdventureGame.Api/AdventureGame.Api.csproj -c Release -o /app/api

# ---- Stage 2: Build the Blazor WASM client ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build-client
WORKDIR /src
COPY src/AdventureGame.Shared/AdventureGame.Shared.csproj src/AdventureGame.Shared/
COPY src/AdventureGame.Client/AdventureGame.Client.csproj src/AdventureGame.Client/
RUN dotnet restore src/AdventureGame.Client/AdventureGame.Client.csproj
COPY src/AdventureGame.Shared/ src/AdventureGame.Shared/
COPY src/AdventureGame.Client/ src/AdventureGame.Client/
RUN dotnet publish src/AdventureGame.Client/AdventureGame.Client.csproj -c Release -o /app/client

# ---- Stage 3: Runtime (nginx + dotnet API in one container) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime

# Install nginx and supervisor
RUN apt-get update && \
    apt-get install -y --no-install-recommends nginx supervisor && \
    rm -rf /var/lib/apt/lists/*

# Copy API
WORKDIR /app
COPY --from=build-api /app/api .

# Copy Blazor WASM static files
COPY --from=build-client /app/client/wwwroot /usr/share/nginx/html

# Override appsettings so ApiBaseUrl is empty (same-origin via nginx proxy)
RUN echo '{}' > /usr/share/nginx/html/appsettings.json

# Copy nginx config for single-container mode
COPY src/AdventureGame.Client/nginx.prod.conf /etc/nginx/conf.d/default.conf
RUN rm -f /etc/nginx/sites-enabled/default

# Copy supervisord config
COPY supervisord.conf /etc/supervisord.conf

EXPOSE 80

CMD ["supervisord", "-c", "/etc/supervisord.conf"]
