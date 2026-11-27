# 021 — Bố cục UI và luồng chuyển màn theo game mẫu     Cỡ: L (chia 4 phần)     Trạng thái: Asset chờ review, luồng UI chờ duyệt
Đang cầm: Claude        AI tham gia: Claude, Codex
Liên quan: 016 (main menu), 019, 020. Nhánh `021-ui-flow` mở 27/11 từ `020-praise-text`.

## Góp ý nhận được (anh lead, người làm chuyển lại 26/11)
Làm artstyle theo game Car Match (Grand Games) về xe và cách bố trí UI; **làm UI trước**. Main menu có logo, khung tiền, ô máu; nút có viền trắng (người làm đưa asset). Cần theo đúng logic Cài đặt, lúc thắng, lúc từ main menu vào bãi. Xe: người làm nói có thể xin asset có giấy phép.

## Phân tích game mẫu
Nguồn: [video lead gửi](https://www.youtube.com/watch?v=5BE7-6zx5k4) (6:41, màn 1–19) và [video bản mới hơn](https://www.youtube.com/watch?v=lyRtE3i51oI) (35:52, màn 1–27, chỉ dùng cho Cài đặt, tab, hộp thoại). Giờ đo trên khung hình YouTube, sai số khoảng ±0,1 s. Không phân tích âm thanh. Ảnh chụp khung: `Logs/021-ui-ref/car-match/` (không commit).

### Bố cục (% theo chiều cao/rộng màn)
| Màn hình | Trên | Giữa | Dưới |
|---|---|---|---|
| Main menu | Một cụm căn giữa ở ~8%: ô avatar, pill tiền có nút +, pill tim (số tim + "Full" hoặc đồng hồ hồi) | Tranh phố, xe nhân vật rộng ~40% ở 52–71% (xe chớp mắt) | Nút "Level N" xanh, viền dày màu kem, rộng ~34%, ở 74–84%. Thanh 5 tab cao ~10%, tab đang chọn nhô lên và có chữ |
| Trong bãi | Pill tiền trái, bánh răng vuông bo phải; bảng "Level N" ở giữa, thấp hơn một hàng (~13%) | Bãi 20–67%, khay 7 ô ở 75–81% | 4 booster ở 90–96%, huy hiệu số ở góc dưới phải |
| Bảng thắng | Tab "Level N" gắn trên mép khung, X đỏ góc phải | "PERFECT!", "Rewards", chồng xu + số | Một nút xanh "Continue". Khung rộng ~56% |
| Cài đặt (bản mới) | Trang đầy màn, tab tiêu đề, X đỏ góc phải | Hàng Notifications (công tắc); khung 3 nút vuông Sound/Music/Haptic (xanh = bật); khung Sign in; nút Support | Terms, Privacy chữ nhỏ |

Màn khó đổi màu cả nút menu và bảng thắng: Hard tím (thưởng 60), Super Hard đỏ kèm ruy băng (thưởng 100).

### Luồng và thời gian
**Thắng**
| Mốc (s) | Việc |
|---|---|
| 0 | Xe cuối rời khay |
| +0,1 → +0,6 | Lớp tối dâng tới gần đen; HUD vẫn lờ mờ bên dưới |
| +0,1 → +1,2 | Logo bật ra: khung xe, chữ C-A-R hiện lần lượt, "MATCH" đập vào |
| +1,2 → +3,0 | Pháo hoa bắn lên rồi nổ |
| +3,0 | Màn 5 trở đi: bảng thắng hiện gần như tức thì trên nền đen, "PERFECT!" đóng xuống sau ~0,4 s |
| Bấm Continue hoặc X | Cắt thẳng về main menu (không mờ) |
| Menu +0 → +1 | Xu nổ ở giữa rồi bay lên pill tiền, số nhảy dần, pill lấp lánh |
| Khi chạm mốc | Tự sang trang Journey (~+1,2 s) → xe chạy lên mốc → "Rewards! Tap to Claim" → quà bay về HUD |

Màn 1–3: dọn xong bãi là sang bãi kế, không lớp tối, không menu. Màn 4: logo + pháo hoa → menu lần đầu, không có bảng thưởng.

**Menu → bãi**
| Mốc (s) | Việc |
|---|---|
| 0 | Bấm "Level N": cắt thẳng sang bãi đã dựng sẵn (< 0,1 s, không mờ đen) |
| +0,1 → +0,35 | Bảng "Level N" rơi từ mép trên xuống |
| ~+0,6 (nếu có cơ chế mới) | Lớp tối + bảng minh họa động có bàn tay + một câu chữ |
| ~+2,5 sau khi mở bảng | Nút "Continue" mới hiện; bấm thì bảng tắt ngay |

**Cài đặt và tab**
- Bản trong video lead gửi: bánh răng là một tab ở thanh dưới. Bản mới: bánh răng ở góc phải trên main menu.
- Bản mới: mở và đóng Cài đặt đều là cắt thẳng, không hoạt hình.
- Đổi tab ở thanh dưới: hai trang trượt ngang (~0,3–0,4 s), thanh tab đứng yên.
- **Chưa thấy trong cả hai video:** bấm bánh răng trong bãi mở ra gì; màn thua/hết chỗ; Cài đặt đóng bằng nút Back của máy.

## So với game mình (ảnh `Logs/021-ui-ref/sheet-ours.jpg`, Play trong Editor 1284×2778, 26/11)
| Điểm | Mình | Mẫu |
|---|---|---|
| Chuyển màn | Mọi lệnh điều hướng, **kể cả Tạm dừng và Thắng**, đều mờ đen 0,1 s + 0,1 s (`GameBootstrap.Transition`, `FeelConfig.transitionSeconds`) | Đổi trang: cắt thẳng. Hộp thoại: lớp tối, không chớp |
| Thắng | Pháo giấy, chờ 0,9 s, bảng 3 nút, "Màn kế" vào thẳng màn sau | Logo + pháo hoa ~3 s, bảng 1 nút, về menu, xu bay |
| Main menu | Tên game bằng chữ, nút MÀN n, 3 tab; không tiền, không tim | Cụm tiền/tim trên, nút viền kem, 5 tab |
| Cài đặt | Hộp giữa màn; công tắc âm/rung lặp ở 3 nơi (menu, chọn màn, tạm dừng) | Một trang duy nhất |
| HUD | Màn trái, đồng hồ giữa, bánh răng phải | Tiền trái, Level giữa, bánh răng phải (không có đồng hồ) |

## Xếp loại góp ý
S ≤ 1 giờ · M ≤ nửa ngày · L ≤ 1 ngày.

| Mã | Loại | Việc | Cỡ | Rủi ro |
|---|---|---|---|---|
| B1 | Lỗi (tự thấy khi chụp) | Màn cuối danh sách: bảng Thắng có hai nút "Chọn màn" | S | Thấp |
| V1 | Cảm giác | Bỏ chớp đen cho Tạm dừng/Thắng/Thua; hộp bật phóng nhẹ; menu → bãi cắt thẳng, HUD trượt vào | M | Chặn chạm xuyên lúc đang chuyển; đồng hồ và Pause phải giữ đúng |
| V2 | Hình | Main menu: logo ảnh, cụm trên cùng, nút viền trắng, chỉnh thanh tab | M | Cần asset; chạy lại menu 016 thì phải chạy lại 019 |
| V3 | Hình | Nút viền trắng cho các hộp thoại | S | Cần asset PNG nền trong, cắt 9 mảnh được |
| V4 | Hình + luồng | Cài đặt: bánh răng góc phải menu, bỏ công tắc lặp; trong bãi bánh răng mở hộp có Âm/Rung/Chơi lại/Về menu | M | Thêm đường "Về menu" trong bãi; không được làm mất tạm dừng đồng hồ |
| V5 | Hình + luồng | Thắng: logo + pháo hoa (chạm để bỏ qua) → bảng sao + thưởng + 1 nút → menu | M | Thêm một lần bấm mỗi màn; đụng `Navigator` và test |
| F1 | Tính năng | Tiền: cộng khi thắng, lưu, pill ở menu, xu bay | M | Không có chỗ tiêu; đụng "chưa làm shop/meta" (PROJECT §1) |
| F2 | Tính năng | Máu: mất khi thua, tự hồi theo giờ, hết thì không chơi được | L | Có thể chặn chính lead khi chơi thử; đổi luật; phụ thuộc đồng hồ máy |
| F3 | Tính năng | Bản đồ Journey, quà mốc, đủ 5 tab | L+ | **Đề nghị không làm** |
| F4 | Cảm giác | Trượt ngang giữa các tab | S–M | Thấp, giá trị thấp |
| X1 | Hình (đợt sau) | Thay xe theo asset được cấp phép | S nếu FBX đúng chuẩn, L+ nếu khác | Chưa có asset/giấy phép; 6 màu 6 dáng, xe 2 và 3 ô, nhìn từ trên nghiêng 58°; mặt trên kính trái quyết định cũ |

## Thứ tự đề xuất
| Ngày | Phần | Gồm | Cần người làm |
|---|---|---|---|
| 26–27/11 | 0 | Cài APK hiện tại lên máy thật, ghi lỗi | Làm trước mọi việc UI |
| 27/11 | A | B1 + V1 | Chơi thử |
| 27–28/11 | B | V4 | Trả lời câu 3 |
| 28/11 | C | V2 + V3 + F1 (pill tiền) | Asset trước sáng 28 |
| 29/11 | D | V5 + xu bay; build APK khi người làm bảo | Chơi thử trên máy |
| 30/11 | Dự phòng | Chỉ sửa lỗi | Nộp |

**Cắt nếu trễ** (cắt trước đứng trước): F3 → F4 → F2 → xu bay → chữ logo bật từng cái → đổi vị trí Cài đặt trong bãi → V5. X1 không vào lịch nếu 27/11 chưa có asset.

## Câu hỏi chờ người làm
1. Tiền và máu: thật hay chỉ hiển thị? Đề xuất: tiền thật (thắng +20, chỉnh trong config, **không** nút +); máu chưa làm. Nếu anh lead bắt buộc có máu: hiện số tim thật, mất khi hết giờ, tự hồi, kèm công tắc "vô hạn" để chơi thử.
2. Sau khi thắng có về main menu như mẫu không? Đề xuất: có, giữ sao trong bảng thắng, một nút "Tiếp tục", thêm nút nhỏ "Chơi lại"; Chọn màn vẫn là một tab.
3. Cài đặt trong bãi: mình không thấy mẫu làm gì. Người làm chụp/quay được màn đó không? Không thì làm theo V4.
4. Asset xe xin của công ty nào, là FBX 3D hay ảnh, khi nào có, có văn bản cho phép không? Xe mẫu có mặt trên kính: bỏ quyết định "không vẽ mặt" hay giữ?
5. Còn bao nhiêu giờ, điều gì không được làm hỏng, và có chấp nhận nộp 12 màn (đang thiếu 13 so với 25) để dồn cho UI không?

## Asset cần người làm đưa
PNG nền trong, ghi nguồn: logo game (không dùng logo Car Match), nút viền trắng (xanh, thêm đỏ nếu có) và nút vuông, icon tim (repo chưa có), khung pill tiền/tim nếu khác `bg_coin_holder`.

## Khảo sát folder asset trước khi bàn giao (27/11)
`AssetCar/Assets` (ngoài repo). `PlayerSettings.json` ghi Grand Games / Car Match, bản 0.0.1112; cấu trúc thư mục theo loại đối tượng Unity: đây là **bản trích từ game đã phát hành**, không thấy file giấy phép hay văn bản cho phép. **Không chép gì vào repo.** Chỉ nhập khi có văn bản của Grand Games (hoặc bên giữ quyền) nêu rõ được dùng trong bài này. Trong lúc chờ: asset tự làm theo tinh thần bố cục (nút viền trắng, pill, tim), không dùng logo hay nhân vật của họ.

### Asset Kenney trong repo (kiểm bằng AssetDatabase, gốc: scene build + Resources)
| Thư mục | File / đang vào build | Xóa được? |
|---|---|---|
| `Art/Cars/Kenney` | 21 / 21 | Không |
| `Art/VFX/Kenney` | 12 / 11 | Không |
| `Assets/kenney_*-sounds`, `kenney_music-jingles` | 325 / 12 | Chỉ file không dùng |
| `Assets/kenney_car-kit` (13 MB) | 251 / 0, không asset nào trỏ tới | Được; giữ lại `License.txt` cạnh `Art/Cars/Kenney` |
| `Art/Mechanics/Kenney` (7 MB) | 213 / 0 | Được nếu xóa kèm 9 prefab/material cũ đang trỏ tới |

Xóa chỉ làm repo gọn, không làm APK nhỏ đi. Trái quyết định 26/11 "giữ asset không dùng" (017) nên chờ người làm duyệt.

## Tiêu chí xong và bậc kiểm
Điền sau khi trả lời câu hỏi. Dự kiến V1 (test điều hướng, tiền) + V2 (Console sạch, Play) + V3 (ảnh Game view trước/sau).

## AssetCar đã gắn trên 021 (27/11)

Người làm xác nhận được dùng các asset này trong bài thực tập và bàn giao phần thay asset cho Codex trên `021-ui-flow`. Phạm vi gồm hình UI hiện có, xe, SFX và texture hạt. Luồng UI mới và các câu hỏi về tiền, máu, tab vẫn ở kế hoạch của Claude.

| Nhóm | File nguồn đã chọn | Điểm gắn trong Squeaky Lane |
| --- | --- | --- |
| UI | `bg_bttn_activate`, `bg_bttn_chat`, `bg_bttn_facebook`, `closeIconGameplay`, `rew_booster01_stroke` từ `Texture2D` | `Play.unity` đổi đúng 13 trường `Image.sprite`. Nút vào màn, xem lại hướng dẫn, tiếp tục và màn kế dùng xanh lá viền kem. Chơi lại dùng cam. Chọn màn trong các hộp thoại dùng xanh dương. Hai nút đóng và một biểu tượng hoàn tác dùng ảnh mới. |
| Xe | `CarVersion-1/2/3/4/5/6-V3.glb` lần lượt cho xanh dương, vàng, xanh lá, tím, cam, hồng | Sáu prefab xe 2 ô trong `Prefabs/Cars`. Mỗi xe có một mesh thân và bốn mesh bánh. Kích thước hình, collider và vị trí bùn lấy theo prefab cũ. Sáu material trên `CarCatalog` dùng atlas với UV riêng cho xe mới. Xe tải 3 ô dùng phần colormap Kenney ở UV cũ. |
| SFX | `UI Button 5`, `Active Car Click 4`, `Passive Car Click 1`, `Correct Answer 2fast 2`, `Super Undo 4 wush-pop fun`, `win_screen`, `level_lose`, `magic_streak_up`, `magic_streak_full` từ `AudioClip` | `SfxCatalog` đổi lần lượt `Ui`, `SelectCar`, `StopWall`, `Exit`, `Undo`, `Won`, `Lost`, `ComboUp`, `ComboBig`. Các ô volume, cooldown, pitch giữ theo catalog. |
| VFX | `small_shine`, ô đầu của `smoke dissolve3`, `sparkle3`, `star` từ `Texture2D` | Trong `CoreFeel.prefab`, đổi `ParticleSystemRenderer.sharedMaterial` cho Nước, Hơi và bụi, Sao, Pháo giấy. `UiTheme.comboSpark` dùng `sparkle3`. Số hạt và cách phát theo prefab và `FeelConfig` đang có. |

Các GLB hiệu ứng xe tìm thấy chỉ chứa tên node. Texture được nối với hệ hạt đang chạy của Squeaky Lane. Âm rửa đặc trưng vẫn dùng asset hiện tại. Mặc định chọn sơn nguồn cho xe, giữ sáu màu theo cổng, giữ asset Kenney cũ để dễ đối chiếu. `StopWall`, `Exit`, `Won`, `Lost`, `ComboUp` được cắt và vuốt đuôi lần lượt còn 0,40 / 0,43 / 1,20 / 1,14 / 1,10 giây theo ngưỡng test âm thanh hiện có.

- **V2 qua Unity MCP.** EditMode 594 đạt, 0 lỗi, 1 bài Profiler thủ công bỏ qua theo cấu hình test. Sau khi sửa thứ tự mặt tam giác đã vào lại Play. Màn 19 dựng 13 xe đủ sáu màu và mở được Tạm dừng. Màn 20 dựng xe tải xanh dương 3 ô. Màn 1 chơi thắng sau hai nước với tiến trình tạm trong bộ nhớ, ghi nhận tiếng xe ra cổng và tiếng thắng đang phát, 40 hạt ở đỉnh và một lời khen hiện cùng lúc. Console không có lỗi/cảnh báo khi kiểm sau Play.
- **V3 qua Simulator.** Ảnh iPhone 1284 × 2778 được lấy trực tiếp từ tab Simulator. Ảnh tổng hợp menu, bãi, tạm dừng ở `Logs/021-assetcar/simulation-review.jpg`. Ảnh riêng màn thắng và xe tải ở `Logs/021-assetcar/simulation-win.jpg` và `Logs/021-assetcar/simulation-truck.jpg`. Chưa chạy APK, chưa đo FPS và chưa nghe đối chiếu trên điện thoại thật.
- **AssetDatabase.** Cả 40 asset mới đều là dependency của Play, có GUID và có `.meta`. Codex không sửa các thay đổi có sẵn ở ProjectSettings và nhóm props City. `PROJECT.md` cập nhật mục 3.1 và sổ việc.

**Bạn cần hiểu**

- `CarView` tách collider khỏi hình. Lưới thân và bánh có thể thay qua prefab mà vùng chạm và logic chiếm ô vẫn theo dữ liệu gốc.
- Hai phần texture trên cùng một atlas cho phép car catalog dùng chung material cho xe 2 ô và xe tải cùng màu mà UV xe tải vẫn đọc đúng colormap Kenney.
- `SfxCatalog` và các material trong `CoreFeel` đổi đầu vào của hệ phản hồi hiện có, nên test kiểm được cả clip ngắn lẫn việc phát đúng sự kiện.

## Đã làm: logo riêng và màn trình diễn thắng (27/11, người làm yêu cầu; nửa đầu V5)
- Logo "Squeaky Lane" vẽ mới (phiếu 021-a). `WinShow` dựng lớp phủ lúc chạy: chờ 0,45 s → tối → nền bọt, 7 chữ, biển LANE → pháo hoa 3 đợt → 3,2 s mở bảng Thắng trên nền tối (alpha 0,92, tới khi rời màn Thắng). Chạm sau 0,4 s hoặc Back để bỏ qua. Bãi tập giữ như cũ.
- Đổi: sao được ghi ngay lúc thắng; mở bảng Thắng sau màn trình diễn không còn mờ đen. Không đổi scene, prefab, Core, Navigator.
- Config mới: `FeelConfig` nhóm 021 (16 số thời gian), `UiTheme` nhóm 021 (hình, màu, cỡ pháo); gán bằng menu `021 UI/Setup Win Logo`.
- Kiểm: EditMode 600 chạy, 599 đạt, 1 bỏ qua (5 ca mới `WinShow021Tests`); Play màn 1: thắng → trình diễn → bảng Thắng, Chơi lại tắt lớp phủ, Console không lỗi code game. Chạy trên working tree đang có cả thay đổi AssetCar chưa commit. Ảnh `Logs/021-logo/chuoi-thang.jpg` chụp bằng cách đặt tuổi lớp phủ khi Editor tạm dừng.
- Chưa kiểm: nhìn chuyển động liền mạch, chạm bỏ qua bằng tay, máy thật, FPS. Chưa làm: bảng một nút, về menu, xu bay (chờ câu 1, 2).
- Phát hiện ngoài phạm vi: màn không tính giờ, bảng Thắng ghi "còn 0 s".

## Trao đổi
2026-11-26 21:55 · Claude → Người làm · [HỎI]: đã phân tích hai video và UI hiện tại, chờ trả lời 5 câu và duyệt thứ tự trước khi code.
2026-11-27 10:24 · Claude → Người làm · [HỎI]: folder AssetCar là bản trích Car Match, chưa nhập; chờ văn bản cho phép hoặc đồng ý tự làm asset, và duyệt danh sách xóa Kenney không dùng.
2026-11-27 10:31 · Người làm → Claude · [BÁO]: nói asset AssetCar được dùng thoải mái, muốn chép phần sẽ dùng; asset Kenney không dùng được giữ lại nếu không làm APK to.
2026-11-27 10:31 · Claude → Người làm · [TRẢ LỜI]: không chép file trích từ Car Match khi chưa có văn bản của bên giữ quyền. Kenney không dùng giữ nguyên (không nằm trong build). Đã vẽ nháp nút viền trắng, pill, tim, xu ở Logs/021-ui-ref/draft (chưa đưa vào Assets), chờ duyệt.
2026-11-27 10:34 · Claude → Người làm · [BÁO]: người làm muốn dùng asset trích cho bài test; vẫn không nhập khi chưa có văn bản. Đề xuất làm đẹp xe và UI đang có, hoặc gói xe có giấy phép; chờ chọn. Commit phần kế hoạch theo yêu cầu.
2026-11-27 10:58 · Codex → Claude · [BÀN GIAO]: trả phần asset cho Claude trên nhánh 021-ui-flow (HEAD 367f578, thay đổi tại working tree). Đã gắn UI, sáu xe 2 ô, chín SFX, bốn texture VFX. V2: Play màn 19/20/1, Console sạch, EditMode 594 đạt/1 bỏ qua theo cấu hình. V3: ảnh Simulator trong Logs/021-assetcar. Còn review phần asset và kiểm trên điện thoại thật.
2026-11-27 11:06 · Claude → Người làm · [BÁO]: đã thấy phần AssetCar Codex gắn (chưa commit). Chưa có văn bản của bên giữ quyền nên Claude không APPROVE, không commit phần đó, kể cả mục AssetCar trong PROJECT.md. Làm logo riêng (phiếu 021-a) và màn trình diễn thắng, commit tách.
2026-11-27 11:11 · Claude → Người làm · [BÁO]: logo và màn trình diễn thắng đã chạy, commit riêng trên 021-ui-flow; chờ chơi thử.
