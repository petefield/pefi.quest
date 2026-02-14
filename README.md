# pefi.quest

An AI-powered text adventure game where ChatGPT acts as the game master, generating scenes with descriptions, action choices, and AI-generated images.

## Architecture

- **Client** -- Blazor WebAssembly SPA with a dark adventure theme
- **API** -- .NET 10 Minimal API backend
- **AI** -- GPT-4o-mini for scene generation, DALL-E 2 for scene images
- **Auth** -- Google sign-in via Azure Container Apps Easy Auth
- **Deployment** -- Single container (nginx + API via supervisord) on Azure Container Apps

```
src/
  AdventureGame.Shared/    # DTOs shared between client and API
  AdventureGame.Api/       # WebAPI: game endpoints, ChatGPT + DALL-E integration
  AdventureGame.Client/    # Blazor WASM: game UI, SSE streaming via JS interop
```

## Features

- Streaming scene descriptions with typewriter effect (SSE via JS interop)
- AI-generated images for each scene (DALL-E 2, 256x256)
- 4-5 action choices per scene
- Scrollable history with thumbnail images
- Game over detection and restart
- Auto-scroll to actions on scene load

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)
- [Docker](https://docs.docker.com/get-docker/) (for containerised dev/deployment)
- An [OpenAI API key](https://platform.openai.com/api-keys)

## Local Development

### Without Docker

1. Create a `.env` file in the project root:

   ```
   OPENAI_API_KEY=sk-your-key-here
   OPENAI_MODEL=gpt-4o-mini
   OPENAI_IMAGE_MODEL=dall-e-2
   ```

2. Run the API:

   ```bash
   cd src/AdventureGame.Api
   dotnet run
   ```

3. Run the client (in a separate terminal):

   ```bash
   cd src/AdventureGame.Client
   dotnet run
   ```

4. Open http://localhost:5180

### With Docker Compose

```bash
docker compose up --build
```

Open http://localhost:8080

## Deployment

The app deploys automatically to Azure Container Apps on every push to `main` via GitHub Actions.

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal JSON (`az ad sp create-for-rbac --json-auth`) |
| `REGISTRY_USERNAME` | Azure Container Registry username |
| `REGISTRY_PASSWORD` | Azure Container Registry password |
| `OPENAI_API_KEY` | OpenAI API key |
| `GOOGLE_CLIENT_ID` | Google OAuth 2.0 Client ID |
| `GOOGLE_CLIENT_SECRET` | Google OAuth 2.0 Client Secret |

### Manual Docker Build

```bash
docker build -t pefi-quest .
docker run -p 8080:80 -e OpenAI__ApiKey=sk-your-key-here pefi-quest
```