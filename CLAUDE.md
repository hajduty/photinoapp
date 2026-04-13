# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

JobTracker is a cross-platform desktop application for job hunting that scrapes Swedish job listings from Platsbanken and uses AI embeddings to recommend jobs based on a CV. The app is built with a Next.js frontend served inside a Photino.NET desktop shell, with a C# backend using SQLite for storage and ONNX models for AI features.

## Development Commands

### Prerequisites
- .NET 8 SDK
- Node.js 20+

### Running in Development

Run both processes concurrently in separate terminals:

```bash
# Terminal 1 — Frontend (Next.js dev server on localhost:3000)
cd JobTracker/UserInterface
npm install
npm run dev

# Terminal 2 — Backend (proxies UI from localhost:3000 in dev mode)
cd JobTracker
dotnet run
```

### Frontend
```bash
cd JobTracker/UserInterface
npm run lint       # ESLint
npm run build      # Production build (copies output to Resources/wwwroot)
```

### Backend
```bash
dotnet test        # Run all xUnit tests (JobTracker.Tests project)
```

### Production Builds
```bash
bash scripts/build-linux.sh [x64|arm64]
bash scripts/build-macos.sh [x64|arm64]
powershell scripts/build-windows.ps1 -Arch x64
```

Each script: builds the frontend, copies it to `JobTracker/Resources/wwwroot`, then does `dotnet publish`.

## Architecture

### Solution Structure
- **`JobTracker/`** — Main desktop app (.NET 8, Photino.NET)
- **`JobTracker.Embeddings/`** — ONNX-based AI services (Jina embeddings, Qwen LLM, BERT)
- **`JobTracker.Tests/`** — xUnit tests (covers job search, embeddings, LLM)

### Request Flow (UI → Backend)

1. **Next.js frontend** sends RPC messages over Photino's WebMessage IPC
2. **`RpcDispatcher`** routes messages to `IRpcHandler` implementations in `Application/Infrastructure/RPC/`
3. **Feature handlers** in `Application/Features/` execute business logic
4. **Domain events** flow via `IEventPublisher` → `IUiEventEmitter` → frontend notifications

### Backend Layout (`JobTracker/Application/`)

```
Features/
  Jobs/              # Job posting CRUD, bookmark, ignore
  JobTracker/        # CV/tracker management
  Classifications/   # Job classification prototypes
  Embeddings/        # Embedding generation & similarity
  Dashboard/         # Stats and heatmaps
  Notification/      # Discord webhook notifications
  JobApplication/    # Application tracking
  Tags/              # Job tagging
  System/            # Settings
Events/              # Domain events
Infrastructure/
  Data/              # EF Core DbContext + migrations (SQLite)
  RPC/               # IPC dispatcher and handler base
  Discord/           # Discord webhook client
  Services/          # Background workers (scraping, embedding)
```

### Frontend Layout (`JobTracker/UserInterface/app/`)

```
features/
  search/            # Job search and filtering
  dashboard/         # Analytics and stats
  settings/          # App settings UI
  notifications/     # Notification management
components/          # Shared components
hooks/               # Custom React hooks
types/               # TypeScript types (mirroring C# models)
utils/               # Utilities
```

### Key Services
- **`AppDbContext`** — EF Core SQLite context; all domain models live here
- **`JinaEmbeddingService`** — Lazy-loads ONNX embedding model with idle-unload timer
- **`QwenService`** — ONNX LLM for job classification
- **`ScrapeService`** — Scrapes Platsbanken via `JobTechScraper`
- **`BackgroundWorker`** — Hosts long-running tasks (scraping, embedding generation)

### IPC Pattern
Photino's `WebMessageReceived` event carries JSON-serialized RPC calls. `RpcDispatcher` deserializes the method name and payload, resolves the matching `IRpcHandler<TRequest, TResponse>` from DI, and sends the result back via `PhotinoWindow.SendWebMessage`.

### AI / Embedding Models
Models are optional and downloaded separately (HuggingFace). The embedding service unloads from memory when idle (configurable timeout). Tests that require models use `SkippableFact` and are skipped when models are absent.

### Platform Notes
- Windows: includes system tray icon (WinForms); app minimizes to tray on close
- Linux: GTK integration via Photino
- macOS: standard Photino window
- Version string is auto-generated from timestamp: `0.0.0-yyyyMMddHHmmss`
