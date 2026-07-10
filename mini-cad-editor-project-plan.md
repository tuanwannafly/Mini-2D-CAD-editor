# Mini 2D CAD Editor — Project Plan
**WPF C# desktop app cho JD TGL Solutions (C++/C# Intern) — Deadline nộp: 30/07/2026**

---

## Giả định & Phạm vi (Scope Assumptions)

- **Persistence:** JSON là bắt buộc (MVP). SQLite là bonus/optional nếu còn thời gian — nếu làm, implement qua interface `IPersistenceService` để show lại **Strategy Pattern** (đã dùng ở Dự Án 1).
- **3D:** Không nằm trong scope. Ghi vào "Post-MVP Backlog" trong README để show định hướng dài hạn, không code.
- **Arc shape:** Vẽ arc bằng chuột khá phức tạp UX-wise (cần 3 điểm hoặc center+2 góc), nên MVP chỉ hỗ trợ tạo Arc qua CLI, không bắt buộc vẽ tay bằng chuột.
- **Điều chỉnh so với bản gốc:** Select + Move dời từ Sprint 1 sang Sprint 2, vì nó phụ thuộc Command Pattern (đã giải thích ở phần trước — tránh viết Undo/Redo 2 lần).

---

## 1. Tech Stack

| Layer | Công nghệ |
|---|---|
| UI Framework | WPF (.NET 8) |
| Pattern | MVVM (CommunityToolkit.Mvvm cho ObservableObject/RelayCommand) |
| Rendering | `Canvas` + `Path`/`Geometry`, `MatrixTransform` cho rotate/scale |
| Testing | xUnit (cho geometry algorithms) |
| Persistence | `System.Text.Json` (bắt buộc), `Microsoft.Data.Sqlite` (optional) |
| Source control | Git + GitHub, Git Flow (giống Dự Án 1) |

---

## 2. Kiến trúc tổng thể

```
Input (Mouse / CLI text)
        │
        ▼
   ICommand (Command Pattern)
   ├─ AddShapeCommand
   ├─ MoveShapeCommand
   ├─ TransformCommand (rotate/scale)
   └─ DeleteShapeCommand
        │
        ▼
ObservableCollection<Shape>  ──► QuadTree (spatial index cho hit-test)
        │
        ▼
   Canvas Rendering (View, data-bound)

GeometryUtils (static, không phụ thuộc UI)
   ├─ LineIntersection
   ├─ PointInPolygon
   └─ ConvexHull (Graham scan)

IPersistenceService
   ├─ JsonPersistenceService
   └─ SqlitePersistenceService (optional)
```

---

## 3. Cấu trúc thư mục

```
CadEditor/
├─ Models/          → Shape, LineShape, CircleShape, ArcShape, RectangleShape, PolygonShape
├─ ViewModels/       → MainViewModel, ShapeViewModel
├─ Views/            → MainWindow.xaml, CommandLinePanel.xaml
├─ Commands/         → ICommand, AddShapeCommand, MoveShapeCommand, TransformCommand, DeleteShapeCommand, UndoRedoManager
├─ Cli/              → CommandParser, CommandGrammar
├─ Geometry/         → GeometryUtils, QuadTree
├─ Persistence/       → IPersistenceService, JsonPersistenceService, SqlitePersistenceService
└─ Tests/            → CadEditor.Tests (xUnit project riêng)
```

---

## 4. Git Strategy

Theo Git Flow (giống Dự Án 1): `main` ← `develop` ← `feature/*`

- `feature/project-setup`
- `feature/shape-models`
- `feature/canvas-rendering`
- `feature/mouse-drawing`
- `feature/command-pattern`
- `feature/cli-parser`
- `feature/select-move`
- `feature/transform-rotate-scale`
- `feature/geometry-algorithms`
- `feature/quadtree`
- `feature/snap-to-grid` *(optional)*
- `feature/persistence-json`
- `feature/persistence-sqlite` *(optional)*
- `feature/ui-polish`

Mỗi feature branch merge vào `develop` qua PR (tự review, viết mô tả PR đàng hoàng — luyện thói quen cho công việc thật). Cuối project, merge `develop` → `main`, tag `v1.0`.

---

## 5. Sprint Plan

### Sprint 1 — Foundation & Rendering (Ngày 1–4)
**Goal:** Có app chạy được, vẽ được shape lên canvas bằng chuột (chưa cần undo, chưa cần select).

**US-1.1: Project Setup**
- *Story:* Là developer, t cần 1 solution WPF có cấu trúc MVVM rõ ràng, để mọi feature sau build trên nền sạch.
- **Branch:** `feature/project-setup`
- **AC:**
  - Solution build không lỗi, chạy được cửa sổ trống
  - Đủ folder structure như mục 3
  - Git repo + Git Flow branches khởi tạo, `.gitignore` chuẩn .NET, push GitHub
  - README stub với mô tả project 2-3 dòng
- **Tasks:** `dotnet new wpf`, cài `CommunityToolkit.Mvvm`, setup `ViewModelBase`, init Git Flow, push scaffold
- **Estimate:** 0.5 ngày

**US-1.2: Shape Models**
- *Story:* Là developer, t cần các class model hình học thuần (không dính WPF), để geometry logic tách biệt khỏi UI.
- **Branch:** `feature/shape-models`
- **AC:**
  - Abstract class `Shape` (Id, IsSelected, StrokeColor...)
  - `LineShape`, `CircleShape`, `RectangleShape`, `PolygonShape`, `ArcShape` kế thừa `Shape`
  - Unit test khởi tạo từng shape đúng property
- **Estimate:** 1 ngày

**US-1.3: Canvas Rendering**
- *Story:* Là user, t muốn thấy shape hiện lên canvas ngay khi được thêm vào collection, để verify trực quan.
- **Branch:** `feature/canvas-rendering`
- **AC:**
  - `ObservableCollection<Shape>` bind vào Canvas qua ViewModel
  - Mỗi shape type render đúng hình dạng (Line→`Line`, Circle→`Ellipse`, Polygon→`Polygon`...)
  - Thêm/xóa item trong collection → canvas tự cập nhật (không cần gọi render thủ công)
- **Estimate:** 1 ngày

**US-1.4: Draw bằng chuột (naive, chưa Undo)**
- *Story:* Là user, t muốn click-kéo chuột để vẽ Line/Circle/Rectangle, để tạo shape trực quan.
- **Branch:** `feature/mouse-drawing`
- **AC:**
  - MouseDown → bắt đầu shape mới; MouseMove → preview động; MouseUp → commit vào collection
  - Polygon: multi-click, double-click hoặc Enter để kết thúc
  - Chưa có undo — chấp nhận được ở bước này (sẽ refactor ở Sprint 2)
- **Estimate:** 1.5 ngày

---

### Sprint 2 — Command Pattern, CLI Parser, Select/Move (Ngày 5–9)
**Goal:** Mọi hành động sửa đổi bản vẽ đều đi qua Command → có Undo/Redo thật sự; gõ lệnh CLI được; select/move object được.

**US-2.1: ICommand Infrastructure**
- *Story:* Là developer, t cần interface `ICommand` (Execute/Undo) và 2 stack (Undo/Redo), để mọi thao tác đều reversible.
- **Branch:** `feature/command-pattern`
- **AC:** `ICommand` với `Execute()`/`Undo()`; `UndoRedoManager` quản lý 2 `Stack<ICommand>`; Ctrl+Z/Ctrl+Y hoạt động
- **Estimate:** 1 ngày

**US-2.2: Refactor Draw → AddShapeCommand**
- *Story:* Là user, t muốn Undo lại được thao tác vẽ vừa rồi.
- **Branch:** `feature/command-pattern`
- **AC:** Toàn bộ luồng vẽ ở US-1.4 giờ đi qua `AddShapeCommand`; Undo xóa shape vừa vẽ, Redo thêm lại đúng shape
- **Estimate:** 0.5 ngày

**US-2.3: DeleteShapeCommand**
- *Story:* Là user, t muốn xóa shape đã chọn và Undo lại được nếu lỡ tay.
- **Branch:** `feature/command-pattern`
- **AC:** Phím Delete xóa shape đang select qua `DeleteShapeCommand`; Undo khôi phục đúng vị trí trong collection
- **Estimate:** 0.5 ngày

**US-2.4: CLI Parser**
- *Story:* Là user, t muốn gõ `LINE 0,0 10,10` hoặc `CIRCLE 5,5 R3` để tạo shape bằng lệnh — đây là differentiator chính so với ứng viên khác.
- **Branch:** `feature/cli-parser`
- **AC:**
  - Grammar hỗ trợ: `LINE x1,y1 x2,y2`, `CIRCLE cx,cy Rr`, `RECT x,y w,h`, `ARC cx,cy Rr startDeg endDeg`, `DELETE id`, `UNDO`, `REDO`
  - Parser sinh ra đúng `AddShapeCommand`/`DeleteShapeCommand` — **tái dùng command từ US-2.2/2.3, không viết logic riêng**
  - Input sai cú pháp → thông báo lỗi rõ ràng, không crash app
  - Unit test cho parser (input hợp lệ / không hợp lệ)
- **Estimate:** 1.5 ngày

**US-2.5: CLI Panel UI**
- *Story:* Là user, t cần 1 ô nhập lệnh + log output, để tương tác kiểu AutoCAD command line.
- **Branch:** `feature/cli-parser`
- **AC:** TextBox nhập lệnh, Enter để submit, log lịch sử lệnh + kết quả phía trên
- **Estimate:** 0.5 ngày

**US-2.6: Select + Move qua Command**
- *Story:* Là user, t muốn click chọn 1 shape rồi kéo thả để di chuyển nó, và Undo lại được nếu di chuyển sai.
- **Branch:** `feature/select-move`
- **AC:**
  - Click vào shape → `IsSelected = true`, highlight viền (hit-test naive bounding-box tạm thời, sẽ thay bằng QuadTree ở Sprint 3)
  - Kéo thả → `MoveShapeCommand` (lưu vị trí cũ/mới để Undo đúng)
  - Click vùng trống → deselect
- **Estimate:** 1.5 ngày

---

### Sprint 3 — Transform, Geometry Algorithms, QuadTree (Ngày 10–14)
**Goal:** Rotate/Scale hoạt động; các thuật toán hình học JD yêu cầu có unit test; hit-test chuyển sang QuadTree cho hiệu năng.

**US-3.1: Rotate & Scale (Affine Transform)**
- *Story:* Là user, t muốn xoay/scale shape đã chọn bằng handle trên canvas.
- **Branch:** `feature/transform-rotate-scale`
- **AC:**
  - Dùng `MatrixTransform` cho rotate quanh tâm shape, scale theo 2 trục
  - Wrap trong `TransformCommand` (Undo/Redo hoạt động đúng)
  - Handle kéo ở góc/cạnh bounding box của shape đang chọn
- **Estimate:** 2 ngày

**US-3.2: Line-Line Intersection**
- **Branch:** `feature/geometry-algorithms`
- **AC:** Hàm `GeometryUtils.LineIntersect(LineShape a, LineShape b)` trả về điểm giao hoặc null nếu song song/không giao; unit test đủ case (giao nhau, song song, trùng nhau, giao ngoài đoạn thẳng)
- **Estimate:** 0.5 ngày

**US-3.3: Point-in-Polygon Test**
- **Branch:** `feature/geometry-algorithms`
- **AC:** Ray-casting algorithm, unit test case điểm trong/ngoài/trên biên polygon lồi và lõm
- **Estimate:** 0.5 ngày

**US-3.4: Convex Hull (Graham Scan)**
- *Story:* Dùng để tính auto-bounding-box khi select multiple shapes.
- **Branch:** `feature/geometry-algorithms`
- **AC:** Input là tập điểm (vertices của các shape đang select), output là hull đúng thứ tự; unit test với tập điểm ngẫu nhiên + edge case (≤2 điểm, các điểm thẳng hàng)
- **Estimate:** 1 ngày

**US-3.5: QuadTree cho Spatial Indexing**
- *Story:* Là user, t muốn click chọn shape mượt kể cả khi canvas có hàng trăm object.
- **Branch:** `feature/quadtree`
- **AC:**
  - `QuadTree` insert/query theo bounding box
  - Thay thế naive loop hit-test ở US-2.6 bằng `quadTree.Query(clickPoint)`
  - Unit test insert/query cơ bản + benchmark nhỏ (so sánh thời gian query naive vs QuadTree với 500+ shapes) — số liệu này rất tốt để show trong CV/interview
- **Estimate:** 1.5 ngày

**US-3.6: Snap-to-Grid / Snap-to-Point** *(Optional — làm nếu còn dư thời gian)*
- **Branch:** `feature/snap-to-grid`
- **AC:** Khi vẽ/di chuyển, tọa độ tự làm tròn về lưới gần nhất (config được grid size); toggle bật/tắt snap
- **Estimate:** 1 ngày — **cắt bỏ đầu tiên nếu trễ tiến độ**

---

### Sprint 4 — Persistence, Polish, Nộp bài (Ngày 15–17)
**Goal:** Lưu/mở bản vẽ, giao diện gọn gàng, README + demo sẵn sàng để nộp.

**US-4.1: Save/Load JSON**
- **Branch:** `feature/persistence-json`
- **AC:** `IPersistenceService.Save(path, shapes)` / `Load(path)`; serialize đúng polymorphic list `List<Shape>` (dùng `JsonPolymorphic` attribute hoặc custom converter); test round-trip save→load giữ nguyên data
- **Estimate:** 1 ngày

**US-4.2: SQLite Persistence** *(Optional bonus)*
- *Story:* Cho JD yêu cầu "làm việc với database" rõ hơn JSON thuần, và show lại Strategy Pattern qua `IPersistenceService`.
- **Branch:** `feature/persistence-sqlite`
- **AC:** `SqlitePersistenceService` implement cùng interface với JSON, switch được qua dropdown "Save as..."; schema đơn giản (bảng Shapes lưu Type + JSON blob của properties)
- **Estimate:** 1 ngày — **cắt nếu trễ tiến độ, JSON đã đủ hit yêu cầu JD**

**US-4.3: UI Polish**
- **Branch:** `feature/ui-polish`
- **AC:** Toolbar icon cho từng tool vẽ, status bar hiện tọa độ chuột + shape đang chọn, keyboard shortcut đầy đủ (Ctrl+Z/Y, Delete, Ctrl+S)
- **Estimate:** 1 ngày

**US-4.4: README + Demo**
- **Branch:** `main` (commit trực tiếp cuối cùng)
- **AC:**
  - README: mô tả, tech stack, kiến trúc (copy sơ đồ mục 2), hướng dẫn chạy, danh sách CLI command hỗ trợ
  - Demo GIF/video ngắn (~30-60s) quay: vẽ bằng chuột, vẽ bằng CLI, undo/redo, rotate/scale, save/load
  - Mục "Future Improvements" liệt kê 3D viewer, thêm lệnh AutoCAD (OFFSET, TRIM, FILLET), layer system — show định hướng dài hạn cho JD này (JD có nhắc ưu tiên ứng viên "định hướng lâu dài trong lĩnh vực CAD")
- **Estimate:** 0.5 ngày

**US-4.5: Final Push & Tag**
- **AC:** Merge `develop` → `main`, tag `v1.0`, kiểm tra lại repo public, link chạy được từ máy sạch (clone mới + build thử)
- **Estimate:** 0.5 ngày

---

## 6. Definition of Done (áp dụng cho mọi story)

- [ ] Code build không warning nghiêm trọng
- [ ] Có unit test cho phần logic thuần (geometry, parser, persistence) — không cần test UI
- [ ] Đã test tay ít nhất 1 lần trên app thật
- [ ] Commit message rõ ràng, đã merge vào `develop` qua PR
- [ ] Không hardcode giá trị test/debug còn sót trong code

---

## 7. Testing Strategy

| Phần | Loại test | Ghi chú |
|---|---|---|
| GeometryUtils (intersection, point-in-polygon, convex hull) | Unit test (xUnit) | Bắt buộc — đây là phần JD nhấn mạnh nhất |
| CLI Parser | Unit test | Test cả input hợp lệ và lỗi cú pháp |
| Command Pattern (Execute/Undo) | Unit test | Test riêng logic, không cần UI |
| QuadTree | Unit test + benchmark nhỏ | Benchmark là điểm cộng khi trình bày CV |
| Persistence (round-trip) | Unit test | Save rồi Load, so sánh data giữ nguyên |
| UI/Canvas rendering | Manual test | Không cần automated UI test, tốn thời gian không cần thiết |

Mục tiêu: tổng số unit test nên **≥ 25-30 test** để so sánh được với Dự Án 2 (66 test) trong CV mà không bị lố thời gian.

---

## 8. Risk & Mitigation

| Risk | Khả năng | Mitigation |
|---|---|---|
| Rotate/Scale (matrix transform) mất nhiều thời gian hơn dự kiến | Trung bình | Cắt Snap-to-grid (US-3.6) trước tiên nếu bị trễ ở đây |
| CLI Parser grammar phình to, khó maintain | Thấp | Giới hạn command set cố định từ đầu (list ở US-2.4), không thêm command mới giữa chừng |
| SQLite tốn thời gian không cần thiết | Cao | Đã đánh dấu Optional — JSON là đủ để pass JD requirement "file để lưu trữ" |
| Trễ tiến độ tổng thể | Trung bình | Buffer Ngày 18-20 dành riêng, không lấn vào thời gian CV/cover letter |

---

## 9. Post-MVP Backlog (ghi vào README, không code)

- Layer system (ẩn/hiện, khóa layer)
- Thêm lệnh CLI: `OFFSET`, `TRIM`, `FILLET`, `MIRROR`
- Export ra DXF (định dạng CAD chuẩn công nghiệp)
- 3D Mesh Viewer (project riêng, đã bàn ở lần trước — không gộp vào đây)

---

## 10. Checklist Timeline Tổng Thể

| Ngày | Sprint | Deliverable chính |
|---|---|---|
| 1–4 | Sprint 1 | App chạy, vẽ tay được, chưa Undo |
| 5–9 | Sprint 2 | Undo/Redo, CLI parser, Select/Move |
| 10–14 | Sprint 3 | Rotate/Scale, geometry algorithms, QuadTree |
| 15–17 | Sprint 4 | Save/Load, UI polish, README, demo, push |
| 18–20 | Buffer | CV, cover letter, luyện nói về project, dự phòng trễ tiến độ |

**Trước khi nộp, check lại:**
- [ ] Repo GitHub public, README đầy đủ với GIF demo
- [ ] CV cập nhật project này (đưa lên đầu nếu apply role này, dùng đúng keyword JD: WPF, C#, geometry, spatial indexing, design pattern)
- [ ] Chuẩn bị sẵn 2-3 câu trả lời: "tại sao dùng QuadTree", "Command Pattern giải quyết vấn đề gì", "sự khác biệt giữa CLI command và mouse-driven action trong kiến trúc của m"
