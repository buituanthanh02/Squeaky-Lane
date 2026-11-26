# 016-f · Tiếng đúng nhịp, camera theo cỡ màn, bùn rõ hơn, bỏ số trên xe     Cỡ: M     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude (giao, review), Codex (làm)
Liên quan: 016-toy-world. Nhánh `016-toy-world`.

## Quyền (người làm yêu cầu 26/11: "cứ giao việc cho Codex mà sửa", ghi đè AGENTS.md §1.3 cho phiếu này)
Được sửa Game, Presentation, UI, tool Editor, test, art, config, scene; được Play và chụp ảnh qua Unity MCP. **Không** đổi Core/luật, package, Project Settings (cần đổi thì ghi [HỎI]), không commit.

## Việc (góp ý người chơi thử)
1. **Tiếng bị trễ.** Hết rè nhưng tiếng xe chạy lên muộn, tiếng đụng cũng muộn so với hình. **Đo trước khi sửa** (thời điểm hình chạm / bắt đầu chạy so với lúc `Play` và lúc nghe thấy trong clip), rồi sửa. Nghi vấn chưa kiểm: `tools/make-sfx.py` `sfx_move` mở đầu 95 Hz + attack 25 ms nên loa nhỏ nghe muộn; im lặng/attack đầu clip Kenney; `preloadAudioData`; DSP buffer 1024 trong `AudioManager.asset` (Project Settings: chỉ đề xuất, không tự đổi); thời điểm `ActionAnimator` bắn `CarStarted`/`Stop` (đâm nên kêu đúng khung xe chạm, có thể phát sớm hơn một chút bằng số trong FeelConfig). Không làm tiếng chồng/rè trở lại (`Audio016Tests`).
2. **Camera theo cỡ màn.** Màn nhiều ô (vd 16, 19) phải **phóng to phần chơi** để dễ chạm, chấp nhận che một phần đường/thảm hầm rửa/phố; màn ít ô (vd 29, 01) đang quá to thì **thu nhỏ một chút** để thấy khu phố. Xem `BoardView.FitCamera`, `WashGateReach`, `UiTheme.streetVisible`, `CameraConfig`. Bắt buộc: cả bãi và mọi cổng nằm trong khung, không bị đồng hồ/booster che; số để trong `CameraConfig`. Kiểm màn 01, 03, 16, 19, 29 trên Simulator iPhone 12 Pro Max, ghi cỡ ô (px) trước/sau; chạm nửa xe vẫn đúng.
3. **Vết bùn** trên xe dày hơn, dễ thấy hơn trên điện thoại (to/đậm/nổi hơn), nhưng màu xe vẫn nhận ra ngay để chọn cổng (xem 014-c `MudSplat`, `FeelConfig.mudSplatScale`).
4. **Bỏ số trên nóc xe** (`UiTheme.hideCarNumbers`; không bỏ nhãn cặp portal).
Khu phố 3D người làm đã duyệt: không đổi.

## Tiêu chí xong
Biên dịch sạch, Console 0 lỗi mới; EditMode Run All không đỏ (mốc **591 / 590 / 0 / 1**), có test cho công thức camera; ảnh trước/sau ở `Logs/016-f/`; ghi nguyên nhân trễ tiếng và số đã đổi (field · cũ → mới). Cập nhật `PROJECT.md` nếu câu nào sai.
**Bẫy:** ghi `.asset` qua MCP xong phải `Resources.UnloadAsset` + `ImportAsset(ForceUpdate)`, kiểm `IsDirty=false`; thoát Play trước khi sửa C#; `execute_code` chỉ C# 6; ngay sau khi vào Play lệnh đầu hay lỗi; Test Runner làm bẩn `Play.unity` → mở scene trống, `git checkout -- Assets/_Project/Scenes/Play.unity`, mở lại; không commit file S4 chưa theo dõi.

## Phản đối kế hoạch
- Phát tiếng va sớm theo một số cố định có thể làm tiếng đi trước hình trên thiết bị có DSP ngắn. `Stop` và phản ứng va hiện bắt đầu cùng clip animation. Ưu tiên nạp sẵn clip và làm rõ đầu tiếng lăn bánh. Chỉ cân nhắc dịch sự kiện hoặc chỉnh DSP sau khi có bản đo chung hình và tiếng trên thiết bị.
- Zoom riêng theo số ô có thể cắt hầm ở rìa bãi. Giữ mức chừa tối thiểu cho khung cổng và chiều cao của vật thể, cho phép cắt thảm ngoài hầm trên bãi lớn như phiếu yêu cầu.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi

1. **Âm thanh.** `tools/make-sfx.py` và `Audio/Sfx/sfx_move.wav` · attack 25 → 6 ms, thêm họa âm 5 và 7 giảm dần trong 40 ms, điểm cắt lọc 800 → 1.200 Hz. Độ dài 260 ms và đỉnh 0,55. `SfxPlayer.cs` · nạp `Move`, `StopCar`, `StopWall` lúc dựng GameRoot. Cooldown 80 ms và cơ chế tắt dần của mỗi cue giữ theo catalog và player hiện có.
2. **Camera.** `CameraConfig.cs`, `BoardView.cs`, `PlayableViewTests.cs` · thêm các field mặc định `wideBoardCells = 25`, `tightBoardCells = 36`, `wideBoardPadding = 0.32`, `washGateFrameReach = 1.1`. Nội suy theo tổng hàng × cột. Bãi lớn ưu tiên kích thước ô, giữ khung hầm và cho phép cắt thảm; bãi nhỏ chừa thêm phố. Mỗi trục tính riêng khoảng chừa cho hầm. Camera vẫn dùng vùng chơi trong safe area để đặt khung.
3. **Bùn.** `FeelConfig.cs` · mặc định và giá trị dự phòng `mudSplatScale` 1,3 → 1,5. `FeelConfig.asset` chưa có field này nên dùng giá trị khởi tạo theo mã. Mỗi chiều tăng ~15%; kiểm bằng mắt lượng màu thân còn nhận được.
4. **Số xe còn hiện.** Thao tác lưu `UiTheme.hideCarNumbers` 0 → 1 bị chặn tại Unity MCP. `BoardView` đã có nhánh bỏ số xe theo field đó; nhãn cặp portal tạo riêng trong `Face`.
- Cập nhật `PROJECT.md` §4 và §7.5. Không đổi scene, prefab, phố 3D, Core, Project Settings hoặc package. Các 54 file S4 chưa theo dõi có từ đầu lượt.

## Kiểm chứng và kết quả

- **Mốc trước khi sửa âm [mã, WAV và log có sẵn].** `CarStarted` phát ở đầu clip di chuyển; `Stop` phát cùng khung gọi `PlayFeel`/phản ứng xe bị đụng, 60 ms trước đỉnh nảy theo `bumpSeconds = 0.12`. `Logs/016-audio/play-log.txt` ghi lịch cũ: lần đầu `Move` ở 0,02 s và cú va ở 2,14 s theo mốc từng lượt. WAV ghép ở thư mục đó không có khung hình đồng bộ hay thời điểm âm ra loa. Hai WAV va lưu sẵn trùng số mẫu với nguồn Kenney (6.116 và 15.500 mẫu ở 44,1 kHz) và vượt 25% đỉnh RMS trong cửa sổ 5 ms đầu. Không có bằng chứng clip va tự chèn khoảng im lặng đầu.
- **Đo waveform trước/sau [offline].** Trong dải lọc mềm 400–1.300 Hz, cửa sổ 5 ms đầu tiên vượt 25% đỉnh RMS của chính clip lăn bánh dịch từ 25 ms về 0–5 ms; RMS dải này trong 20 ms đầu tăng 0,0069 → 0,0964 (toàn thang 1). Ba clip đích trên repo có `preloadAudioData: 0`. Nạp lạnh và DSP là hai nguồn trễ có thể có. DSP 1.024 mẫu tương đương khoảng 21 ms ở 48 kHz cho một buffer; chưa đo số buffer và độ trễ thiết bị.
- **Camera [ước lượng offline].** Dùng khung 1.284 × 2.778, safe area giả định cao 2.535 px, chiếu AABB từ mesh cổng và mô phỏng tia vào collider xe. Cả năm bãi nằm trong vùng chơi, 33/33 khung cổng và 56/56 điểm giữa từng nửa xe đạt. Test Unity mới còn chờ chạy. Kết quả ở `Logs/016-f/camera-estimates.txt`.

  | Màn | Trước (px/ô) | Sau (px/ô) |
  |---|---:|---:|
  | 01 | 190,2 | 173,7 |
  | 03 | 128,4 | 127,7 |
  | 16 | 116,7 | 152,9 |
  | 19 | 107,0 | 136,6 |
  | 29 | 165,7 | 153,0 |

- **Biên dịch [Editor và Roslyn].** Unity tự biên dịch và reload sau thay đổi; đọc Console qua MCP được 0 lỗi/cảnh báo. Biên dịch độc lập Game, Presentation, Editor và EditMode bằng Roslyn đi cùng Unity cũng qua. `git diff --check` qua.
- **Chưa chạy V2/V3.** Chính sách phê duyệt hiện tại từ chối lệnh đổi asset, Refresh, Play, Run All và thực thi mã của Unity MCP. Chưa có EditMode Run All sau sửa, Play trên Simulator, ảnh mới, kiểm HUD/booster trên máy, hoặc clip chung hình và âm để đo độ trễ loa. `Logs/016-f/archive-before-*.png` chứa ba ảnh cũ của màn 01/03/16 làm mốc xem lại.

## Giả định · Bạn cần hiểu

- Tự tin khoảng 85% về phần đã biên dịch và kiểm offline. Chỗ cần xem lại là độ rõ màu xe khi vẩy bùn to hơn và nhịp tiếng va trên loa máy.
- Sự kiện animation quyết định lúc gọi `Play()`. Dữ liệu clip, hàng đợi DSP và thiết bị quyết định lúc âm tới loa.
- Camera trực giao thu/phóng cả bãi. Phần nhìn thấy ở mép giảm khi ô to hơn nên giới hạn khung cổng được áp trước khi tính kích thước camera.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 14:32): APPROVE có sửa
- Đã chạy (MCP): biên dịch, Console 0 lỗi, EditMode **596 · 595 · 0 · 1**; Play Simulator màn 01/03/16/19/29: chạm nửa xe 12/24/42/78/12 điểm đều đúng; ảnh `Logs/016-f/after-*.png`.
- Codex bị chặn lệnh MCP nên Claude bật `UiTheme.hideCarNumbers` 0 → 1.
- [NÊN → đã sửa] màn 03 (ít ô, 2 hầm rửa) ô nhỏ nhất 128 px: thêm `CameraConfig.washGateSceneryReach` 1,8 → 145 px. Cỡ ô: 01 174 · 03 145 · 16 153 · 19 137 · 29 153 px.
- [NÊN → đã sửa] phóng cả cụm vẫn chỉ là chấm: từng mảng `MudSplat` to ×1,6, dày ×1,45 (`WashVfxTools`, chạy lại menu 014), `mudSplatScale` 1,5.
- [HỎI] Trễ tiếng còn lại: Editor đo DSP **1024 × 4 @48 kHz ≈ 85 ms** (`ProjectSettings/AudioManager.asset`). Đề xuất DSP Buffer Size → *Good latency* (512 ≈ 43 ms) hoặc *Best latency* (256 ≈ 21 ms, dễ lụp bụp trên máy yếu). Chờ người làm duyệt vì là Project Settings.
- Chưa kiểm: đo trễ trên máy thật, vệt bánh bùn (người làm có thể muốn nói "vệt" chứ không phải vết trên xe).

## Trao đổi
2026-11-26 14:16 · Claude → Codex · [BÀN GIAO]: giao 016-f; nhánh `016-toy-world` HEAD c261d89. Claude không lái Editor trong lúc bạn làm.
2026-11-26 14:27 · Codex → Claude · [HỎI]: Unity MCP chặn ghi asset. Nhờ Claude bật `UiTheme.hideCarNumbers` trong Inspector, xác nhận nhãn portal, rồi chạy Run All và Play/chụp Simulator. Cần một clip có hình và âm cùng mốc để quyết định có phải bù tiếng va.
2026-11-26 14:27 · Codex → Claude · [BÀN GIAO]: `016-toy-world` HEAD e233943; giao bản sửa âm/camera/bùn và test camera, đã biên dịch và kiểm offline, Console MCP 0 lỗi/cảnh báo. Còn toggle số xe và kiểm V2/V3 như trên.
2026-11-26 14:32 · Claude → Người làm · [HỎI]: duyệt đổi DSP Buffer Size để giảm trễ tiếng?
