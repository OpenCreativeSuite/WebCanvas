# WebCanvasDotNet

A desktop static-site CMS and visual website builder built with **Donkeh UI + CSX**.
It combines content management, page design, data modelling, automation, development
tools and a static publisher in one calm workspace.

The interface follows a simple rule: **sophisticated capability, simple surface**.
The application shell is inspired by VS Code, the design workspace by Illustrator,
and the spacing and restraint by Apple. Every screen exposes only what belongs to
the current task.

## Requirements

- .NET 10 SDK
- A desktop environment (Windows, macOS or Linux)

## Build and run

```powershell
csx build          # transform .cx files and build
dotnet run         # launch the desktop app
csx run            # transform and launch in one step
```

When changing stylesheets, force the embedded CSS to refresh:

```powershell
csx build
dotnet build --no-incremental
```

### Startup arguments

Useful for automation and smoke tests:

```
dotnet run -- --create <parent-folder> --name "My Site" [--blank] [--build]
dotnet run -- --resume                      # reopen the last project
dotnet run -- --project <project-folder> [--mode Design|Content|Blocks|Data|Automation|Development]
dotnet run -- --project <project-folder> --mode Development --section Build --build
dotnet run -- --project <project-folder> --page about --node <node-id> [--block <block-id>]
dotnet run -- --project <project-folder> --mode Content --type Post --record <entity-id>
dotnet run -- --project <project-folder> --mode Data --type Post --show-on-site Post
dotnet run -- --light | --dark [--scale 100|115|130]
dotnet run -- --palette [--query "page"]
dotnet run -- --create-dialog | --browse | --new-page
dotnet run -- --screenshot <path>          # host captures frame 120 and exits
```

Without `--project`, `--create` or `--resume`, the application opens on the
project hub with recent projects, Create and Open folder.

## Tests

The test project lives in `tests/WebCanvasDotNet.Tests` and uses Donkeh's headless
component harness:

```powershell
csx build
dotnet test tests/WebCanvasDotNet.Tests/WebCanvasDotNet.Tests.csproj
```

- `tests/.../Components` has one test class per visual component.
- `tests/.../Core` covers project persistence, slug helpers, static generation,
  entity routes, editing/undo and the local preview server.
- `tests/.../Structure` enforces the component folder convention and checks the
  GitHub workflows.

The workflow checks out `barely-sentient/donkeh-ui` into the sibling
`UI Framework/CSS-Engine` path expected by `WebCanvasDotNet.csproj`, then runs the
same commands in `.github/workflows/tests.yml`.

## What is in the application

The mode rail opens VS Code-style tabs. Tabs remember the area and item they point
at, can be closed, and are restored by clicking the matching sidebar entry.

- **Content** — the default relationship map shows how data types connect. Click a
  type for a searchable, filterable collection with a focused editor, required-field
  validation, relationships, duplicate/delete and CRUD.
- **Design** — pages are nested trees plus an Elementor-style widget palette. Drag
  a widget onto the canvas and a browser-style drag image follows the pointer. The
  nearest valid drop site outlines itself and shows an insertion line before the
  widget is released; clicking adds inside the current selection. The palette
  includes hero, grid, image-with-text, image-with-title, subtitle, buttons, stats,
  FAQ, cards, collections, text links, code blocks and YouTube/generic embeds.
  Every page is a JSON tree; the canvas and the published site are both rendered
  from that same tree. **Preview** builds the site first, then opens the compiled
  page in a new work tab rendered by Donkeh's embedded Chromium `webview`. Previews
  are served from a local HTTP origin so YouTube and other embeds receive the
  referrer they require.
- **Blocks** - a separate workspace for reusable blocks: a library of Theme and
  Content blocks, a block editor, and a widget palette. The core prefab kit adds
  full-width header and footer layouts (simple, split, centred, minimal,
  three-column, newsletter and more) to the current block or as a new block.
  Blocks can contain other blocks (cycles are rejected). Drag any block from the
  sidebar into a page or another block.
- **Data** — types, fields, relationship explorer, public JSON preference, and a
  one-click **Show on site** action that adds a collection for a type to a page.
  Each type can also nominate a **detail page**; records then publish at
  `/<plural>/<slug>` (or `/<plural>/<id>` when there is no slug field) and the
  page content can use `{{title}}`, `{{slug}}` and `{{field}}` tokens.
- **Automation** — event browser, a visual event → handler → action map, ordered
  conditions/actions, enable/pause and a diagnostic test log.
- **Development** — project overview and health, live theme editor with theme
  blocks, plugin discovery, generated API catalogue and the static build console.

The startup project hub offers Create, Open folder, recent projects and a complete
Aster & Oak sample. Folder selection uses **OpenCreativeSuite.Components.Files**;
one `FileBrowserDialogHost` is mounted at the root, dialogs are themed to match the
app, and the same picker is used for creating and opening projects. The application
does not silently reopen the last project; use a recent project or `--resume`.

## Project layout

A created project contains:

```
.wc                          versioned project manifest
app/design/pages/*.json      page component trees (t / p / c)
app/design/blocks/*.json     reusable Theme or Content blocks
app/design/themes/           theme.json + generated style.css
app/data/schemas/*.json      data type definitions
app/data/<plural>/entities/  authoritative entity JSON files
app/automation/handlers.json event handlers, conditions and actions
app/code/<vendor>/<plugin>/  plugin.json, Scripts, Styles, Components
app/api/index.json           generated API definition
assets/                      site assets
build/site/                  static output
```

Page nodes are a small, readable JSON shape:

```json
{
  "id": "node_1",
  "tag": "section",
  "className": "site-hero site-shell",
  "children": [
    { "id": "node_2", "tag": "h1", "text": "Interfaces that feel inevitable." }
  ]
}
```

Pages can reference a reusable block:

```json
{ "tag": "block", "attrs": { "block": "block_site_header" } }
```

`collection` nodes embed records at build time:

```json
{ "tag": "collection", "className": "collection-grid", "attrs": { "type": "Post", "limit": "3" } }
```

Navigation nodes (`attrs.nav = "main"`) are generated from the pages marked
*Show in navigation*, so adding a page updates every page's navigation.

## Static output

The Build screen writes an ordinary static site into `build/site`:

- HTML pages with relative links (works at a domain root, a subpath or locally)
- `site.css` generated from the project theme plus the shared block stylesheet
- `data/<plural>/index.json` and one JSON file per record
- `search.json`, `sitemap.xml`, `robots.txt`, `404.html`
- copied assets and plugin scripts, appended plugin styles
- a dependency-free `runtime.js`

Set the site URL in the project manifest to make canonical links and the sitemap
absolute. Blocks are expanded into the page HTML at build time, so every page that
references a block inherits edits to it. Types marked **Publish JSON** off are
omitted from `data/` and the generated API routes; a collection already placed on a
page still renders its records.

A page assigned as a type’s detail page is a route template: it is not published at
its own address, and the builder writes one HTML page per record. Collection cards
link to those record pages automatically, and the sitemap and search index include
them. The detail page can be deleted only after the type is pointed elsewhere, so
records never lose their route.

## Architecture

- `src/Components/**/*.cx` — every application view is authored in **CSX** with
  literal `<div>`, `<button>`, `<input>`, `<select>` markup. UI helpers in
  `src/Components/Ui/Ui.cx` are also CSX markup; there is no hand-written element
  builder API in the UI layer.
- Each component lives in `src/Components/{Name}/` with `{Name}.cx`, `{Name}.css`
  and `{Name}.md`. The markdown file explains the component and how it connects to
  `StudioSession`. Shared control primitives are in `Ui`, while app-wide layout,
  tokens and late overrides stay in `src/Styles`.
- `src/Core/*.cs` — project model, disk I/O, starter content, static publisher,
  studio state, and the single mutable `StudioSession`.
- `src/Styles/*.css` — tokens (light and dark), shell, workspace layouts and design
  canvas styles. Embedded through `src/Stylesheets.cs`.
- `SiteBlocks.cs` — the shared site stylesheet used by the design canvas and the
  generated static site, so what you design is what you publish.
- `AssemblyBridge.cs` — resolves the component package's Donkeh.UILab reference to
  the local framework checkout used by this repository.

State lives in `StudioSession`; components read it during render and call methods
from event handlers. `Refresh` schedules a re-render through one state tick. The
design canvas renders the page tree through `SiteRenderer`, which is the only place
dynamic tags are constructed (the JSON tree decides the tag name).

The relationship and automation maps use ordinary document boxes with CSS connector
lines (`GraphView`), so they theme correctly without a separate canvas. Application
dropdowns use the custom `Picker` component instead of native `<select>` popups,
whose light platform chrome cannot be themed.

## Theme and visual language

Light is the default; dark is a separately designed palette, not an inversion. All
colours, lines, radii, shadows and scrollbars are custom properties on the app root.
The published site has its own independent theme tokens, so the studio can be dark
while the site is warm and light.

The shell uses a 58px activity rail, a contextual sidebar, a prioritised central
workspace, and an inspector that appears only for Design and Data. Panels collapse
from their headers or with `Ctrl+B` / `Ctrl+Shift+I`.

Interface text is scaled to **115%** by default so it remains readable on modern
high-density displays. Development → Overview → Accessibility offers Default,
Comfortable and Large (100/115/130%), and the same options are in the command
palette. Blocks are explicitly categorised as **Theme** or **Content**, which lets a
theme be tested against header/footer chrome and page material separately.

## Keyboard

```
Ctrl+K / Ctrl+P / F1   Command palette
Ctrl+O                 Open a project folder
Ctrl+1 … Ctrl+6        Content / Design / Blocks / Data / Automation / Development
Ctrl+B                 Toggle sidebar
Ctrl+Shift+I           Toggle inspector
Ctrl+Shift+B           Build site
Ctrl+Shift+L           Toggle app theme
Ctrl+Z / Ctrl+Shift+Z  Undo / redo page edits
Ctrl+C / Ctrl+X        Copy / cut the selected block
Ctrl+V                 Paste inside the selected container, or after it
Ctrl+D                 Duplicate the selected block
Delete / Backspace     Delete the selected block
Ctrl+S                 Save everything
Ctrl+W                 Close tab
Esc                    Close overlay / clear selection
```
