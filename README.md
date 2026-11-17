# DOT (Document Optimizer Tool)

A cross-platform .NET MAUI + Blazor Hybrid desktop/mobile client for batch-editing Word/technical documents through a set of reusable, shareable **routines**.

Instead of opening and fixing dozens of documents by hand, you build a routine once (e.g. "recolor every hex value in this range", "replace this image everywhere", "rewrite these paragraphs", "translate this file") and apply it across many uploaded files in one pass. Each file is processed independently and the edited result is downloaded automatically, with per-file success/warning/error status.

## Features

- **Batch upload & processing** — drag and drop (or pick) multiple files and apply the same edit routine to all of them, with per-file progress and status (success / warning / error), including graceful handling of oversized or failed files.
- **Routine system** — routines are typed, serializable configurations that describe *what* to change:
  - `RoutineColorGap` — remaps colors/styles (font size, bold, italic, hex color) for any value that falls between a start/end color range, optionally scoped to body elements, headers and/or footers.
  - `RoutineEditTexts`, `RoutineEditParagraphs` — targeted text/paragraph edits.
  - `RoutineReplaceImages` — bulk image replacement inside documents.
  - A translation routine (`translate_file` endpoint) for whole-document translation.
- **Shareable routines** — any routine can be serialized to JSON and shared via the OS share sheet, so a routine built once can be reused by someone else or on another device.
- **Client/backend split** — the MAUI/Blazor app is a thin UI client; the actual document-editing work happens behind a local HTTP API (`mark_file`, `edit_texts`, `edit_color_gap`, `edit_paragraphs`, `replace_file_imgs`, `translate_file`) that the client posts multipart file uploads to and streams the processed file back from.
- **Localization** — UI strings are resource-based (`.resx`) with English, Portuguese, Spanish and French translations, selectable at runtime and persisted via `Preferences`.
- **Dynamic forms** — generic `Forms`/`Table` model layer (`Models/Forms`, `Models/Tables`) renders configuration UI for routines and other structured data without hardcoding a layout per routine type.

## Tech stack

- **.NET MAUI** (net9.0) — native shell, file picking, share sheet, per-platform downloads folder resolution (Windows / Android / macOS / iOS).
- **Blazor Hybrid** (`BlazorWebView`) — the actual UI (pages, layout, components) is Razor/Blazor rendered inside the native shell.
- **C#** throughout, with a small reflection-based native WebView check on Windows for diagnostics.
- Bootstrap Icons (vendored) for iconography.

> Note: this repository contains only the MAUI/Blazor client. It expects a document-processing HTTP API available at `http://127.0.0.1:5000` implementing the routine endpoints listed above; that backend is a separate service and is not part of this repo.

## Project layout

```
DOT User Interface/
  Components/
    Layout/      # app shell, nav, settings, generic form/table/file-picker widgets
    Pages/       # Home, Editor, Generator, Marker, Translator, Help
  Models/
    Routines/    # routine types (color gap, text edit, paragraph edit, image replace) + factory
    Forms/       # generic dynamic form model + factory
    Tables/      # generic dynamic table model + factory
    ProcessFiles.cs   # multipart upload orchestration + per-file status tracking
  Resources/Location/  # Strings.resx (en/pt/es/fr)
  wwwroot/             # static web assets served by the Blazor WebView
```

## Running it

Requirements:
- Visual Studio 2022+ with the **.NET MAUI** workload installed, or the .NET 9 SDK with the MAUI workload (`dotnet workload install maui`).

```bash
# restore & build
dotnet build "DOT (Document Optimizer Tool).sln"

# or open the .sln in Visual Studio and run the "DOT User Interface" project
# (Windows target by default: net9.0-windows10.0.19041.0)
```

A compatible document-processing backend must be running on `http://127.0.0.1:5000` for the processing actions to succeed.

## License

MIT — see [LICENSE](LICENSE).
