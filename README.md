# Mini 2D CAD Editor

A lightweight 2D CAD editor built with **WPF (.NET 10) + MVVM** featuring mouse drawing, a CLI command parser, undo/redo, geometry algorithms, spatial indexing with QuadTree, and dual persistence (JSON + SQLite). Created as a portfolio project demonstrating desktop application architecture, design patterns, and computational geometry.

![.NET](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-blueviolet)
![xUnit](https://img.shields.io/badge/Test-xUnit-brightgreen)

## Features

### Drawing Tools (Mouse)
| Tool | Shortcut | Description |
|---|---|---|
| **Select** | `S` | Click to select, drag to move |
| **Line** | `L` | Click-drag to draw a line segment |
| **Circle** | `C` | Click-drag from center to edge |
| **Rectangle** | `R` | Click-drag from one corner to opposite |
| **Polygon** | `P` | Multi-click to place vertices; double-click or `Enter` to finish |

### Transform
- **Rotate**: Drag the rotation handle (green ring) around the selected shape
- **Scale**: Drag corner or edge handles of the selection bounding box
- Both operations wrap through `TransformCommand` — fully undo/redo capable

### CLI Command Line
An AutoCAD-style command-line panel supports the following grammar:

```
LINE x1,y1 x2,y2          e.g. LINE 0,0 10,10
CIRCLE cx,cy Rr           e.g. CIRCLE 50,50 R20
RECT x,y w,h              e.g. RECT 10,10 80,60
ARC cx,cy Rr start end    e.g. ARC 100,100 R40 0 180
DELETE <id>               e.g. DELETE abc123-def456-...
UNDO
REDO
```

The parser reuses the same Command Pattern infrastructure as mouse actions — all CLI-created shapes support undo/redo.

### Undo / Redo
- Full Command Pattern with `IEditorCommand` → `Execute()` / `Undo()`
- `UndoRedoManager` maintains dual stacks
- Keyboard: `Ctrl+Z` (Undo), `Ctrl+Y` (Redo), `Delete` (delete selected)
- New operations clear the redo stack, preventing stale history

### Hit Testing — QuadTree
- Spatial indexing replaces naive O(n) hit-testing
- QuadTree with configurable capacity (default 8) and max depth (default 10)
- `query(point)` returns candidate shapes intersecting the hit-test radius

### Snap
- **Snap-to-Grid**: Rounds coordinates to configurable grid size (default 20)
- **Snap-to-Point**: Snaps to endpoint vertices of existing shapes
- Both toggleable from the toolbar

### Persistence
| Format | Class | Status |
|---|---|---|
| **JSON** | `JsonPersistenceService` | Required (MVP) |
| **SQLite** | `SqlitePersistenceService` | Optional bonus |

`IPersistenceService` + `PersistenceServiceFactory` demonstrates **Strategy Pattern** — switch formats at runtime via a dropdown. Save/load with `Ctrl+S`.

### Geometry Algorithms
- **Line-Line Intersection**: Parametric segment intersection, handles parallel/collinear/no-intersect cases
- **Point-in-Polygon**: Ray-casting algorithm with on-edge detection
- **Convex Hull**: Graham scan — computes auto-bounding-box for multi-shape selection
- All implemented in pure static `GeometryUtils` (no UI dependency) with full **xUnit** coverage

## demo


## Architecture

```
Input (Mouse / CLI text)
        │
        ▼
   IEditorCommand (Command Pattern)
   ├─ AddShapeCommand
   ├─ MoveShapeCommand
   ├─ TransformCommand (rotate/scale)
   └─ DeleteShapeCommand
        │
        ▼
ObservableCollection<Shape>  ──► QuadTree (spatial index for hit-test)
        │
        ▼
   Canvas Rendering (WPF Canvas, data-bound)

GeometryUtils (static, no UI dependency)
   ├─ LineIntersection
   ├─ PointInPolygon
   └─ ConvexHull (Graham scan)

IPersistenceService (Strategy Pattern)
   ├─ JsonPersistenceService
   └─ SqlitePersistenceService
```

## Project Structure

```
CadEditor/
├─ Models/           Shape, LineShape, CircleShape, ArcShape,
│                    RectangleShape, PolygonShape, QuadTree,
│                    SnapService, Point2D, BoundingBox
├─ ViewModels/       MainViewModel, ViewModelBase, DrawingTool
├─ Views/            MainWindow.xaml(.cs), Converters/
├─ Commands/         IEditorCommand, AddShapeCommand, MoveShapeCommand,
│                    TransformCommand, DeleteShapeCommand, UndoRedoManager
├─ Cli/              CommandParser, CliCommand, CliParseResult
├─ Geometry/Utils/   GeometryUtils
└─ Services/         IPersistenceService, JsonPersistenceService,
                     SqlitePersistenceService, PersistenceServiceFactory

Tests/
├─ Models/           QuadTreeTests, ShapeTests, SnapServiceTests
├─ Commands/         UndoRedoManagerTests, TransformCommandTests,
│                    MoveShapeCommandTests, DeleteShapeCommandTests,
│                    CommandParserTests
├─ Geometry/         GeometryUtilsTests
└─ Services/         JsonPersistenceServiceTests,
                     SqlitePersistenceServiceTests
```

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (Windows Presentation Foundation) |
| Architecture | MVVM with `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand) |
| Rendering | Canvas + Shape elements, `MatrixTransform` for rotate/scale |
| .NET SDK | .NET 10.0 (targeting `net10.0-windows`) |
| Testing | xUnit + `Microsoft.NET.Test.Sdk` + `coverlet.collector` |
| Persistence | `System.Text.Json` + `Microsoft.Data.Sqlite` |
| Version Control | Git + GitHub |

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (WPF requires a Windows target framework)

## Quick Start

```bash
# Clone
git clone https://github.com/tuanwannafly/Mini-2D-CAD-editor.git
cd Mini-2D-CAD-editor

# Build
dotnet build

# Run
dotnet run --project CadEditor

# Test
dotnet test
```

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save drawing |
| `Delete` | Delete selected shape |
| `S` | Select tool |
| `L` | Line tool |
| `C` | Circle tool |
| `R` | Rectangle tool |
| `P` | Polygon tool |
| `Enter` | Submit CLI command / finish polygon |

## Post-MVP Roadmap

These items are documented for future direction but not implemented:

- **Layer system** — show/hide and lock layers
- **Additional CLI commands**: `OFFSET`, `TRIM`, `FILLET`, `MIRROR`
- **DXF export** — industry-standard CAD interchange format
- **3D Mesh Viewer** — separate project (discussed, not merged into this repo)

## License

MIT