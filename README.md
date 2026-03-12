# JobTracker
A desktop app to help with job applications. Scrapes listings from Platsbanken, recommends jobs based on your CV. Preconfigured for developer jobs. 

<img width="2559" height="1348" alt="JobTracker screenshot" src="https://github.com/user-attachments/assets/3c0a5399-6554-4850-bcd0-38115a8c3778" />

## Download
Grab the latest release for your platform from the [Releases](../../releases) page.

| Platform | File |
|----------|------|
| Windows x64 | `JobTracker-*-win-x64.zip` |
| Windows arm64 | `JobTracker-*-win-arm64.zip` |
| Linux x64 | `JobTracker-*-linux-x64.tar.gz` ⚠️ untested |
| Linux arm64 | `JobTracker-*-linux-arm64.tar.gz` ⚠️ untested |
| macOS x64 | `JobTracker-*-osx-x64.dmg` ⚠️ untested |
| macOS arm64 | `JobTracker-*-osx-arm64.dmg` ⚠️ untested |

> **macOS:** Right-click → Open on first launch to bypass Gatekeeper (app is unsigned).  
> **Linux:** Extract the archive and run `install.sh`.  
> **Linux & macOS builds are compiled but untested.**

## Features
- Scrapes jobs from Platsbanken
- Webhook notifications on new matches
- Fully local

## Planned
- Job recommendations based on CV & keywords
- Email API integration, track application status & read email intent to update status "Interview, Offer, Rejected"
- Configurable scraping, give users ability to add more job boards from the UI & setup scraping rules

## Tech Stack

| | |
|---|---|
| **UI** | React, Mantine, Tailwind |
| **App** | C#, Photino.NET, Entity Framework, SQLite |
| **"AI" stuff** | ONNX Runtime, Jina Embeddings |

## AI Features Setup

The job recommendation & email intent classification features require a local embedding model.

**1.** Download the following files from [jinaai/jina-embeddings-v5-text-nano-classification](https://huggingface.co/jinaai/jina-embeddings-v5-text-nano-classification):
- `config.json`
- `model.onnx`
- `model.onnx_data`
- `tokenizer.json`

**2.** Place them in:
```
# Windows
%AppData%\JobTracker\Models\jina-embeddings-v5-text-nano-classification\

# macOS
~/Library/Application Support/JobTracker/Models/jina-embeddings-v5-text-nano-classification/

# Linux
~/.config/JobTracker/Models/jina-embeddings-v5-text-nano-classification/
```

The app will automatically detect the model on next launch.

## Building from Source
**Prerequisites:** .NET 8, Node.js 20+

```bash
# Clone
git clone https://github.com/hajduty/photinoapp
cd photinoapp

# Frontend
cd JobTracker/UserInterface
npm install
npm run dev

# Backend (separate terminal)
cd JobTracker
dotnet run
```

Or use the build scripts to produce a release build:

```bash
bash scripts/build-linux.sh    # Linux
bash scripts/build-macos.sh    # macOS
.\scripts\build-windows.ps1    # Windows (PowerShell)
```