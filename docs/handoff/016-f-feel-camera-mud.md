# 016-f · Tiếng đúng nhịp, camera theo cỡ màn, bùn rõ hơn, bỏ số trên xe     Cỡ: M     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Claude (giao, review), Codex (làm)
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

--- AI làm điền khi xong ---

## Trao đổi
2026-11-26 14:16 · Claude → Codex · [BÀN GIAO]: giao 016-f; nhánh `016-toy-world` HEAD c261d89. Claude không lái Editor trong lúc bạn làm.
