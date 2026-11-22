# Dữ liệu chuẩn từ prototype — việc 005, bổ sung độ phủ ở 006

Đây là **31 màn cũ để kiểm chứng luật**, không phải bộ 25 màn cuối của Squeaky Lane. Mọi file ở ngoài `Assets`, không có `.meta`, không vào APK. Nguồn chỉ đọc: `../reference/prototype/src/logic.js`, `src/levels.js` và `levels-export/level_NN.json`.

## Chạy từ thư mục gốc repo

Cần Node có sẵn (đã dùng v24.14.1), không cần npm/package bên ngoài hoặc Unity. Mỗi lệnh chạy riêng:

```text
node docs/golden/tools/export.js
node docs/golden/tools/make-traces.js
node docs/golden/tools/verify.js
```

`export.js` và `make-traces.js` ghi lại các JSON tương ứng. `verify.js` chỉ đọc/kiểm dữ liệu, trả exit code khác 0 khi lệch; báo màn/chuỗi/nước và nhóm trường lỗi. Các lệnh định vị file theo `__dirname`, không phụ thuộc thư mục làm việc. Không sửa nguồn trong `docs/reference/prototype/`.

## Level JSON (`levels/level_NN.json`)

- `id`: 1–31 theo thứ tự `CW_LEVELS`; `name`, `cols`, `rows` giữ từ nguồn.
- `gates`: `{side,i,color}`, cạnh bãi `t|b|l|r`; `color` là khóa chữ của nguồn, thêm `water` cho cổng nước.
- `cars`: `{axis,r,c,color,len,truck,mud}`; axis `h|v`, r/c là góc trên-trái. Mặc định ghi tường minh: len=2, truck=false, mud=0.
- `cones`, `muds`, `sprays`, `tables`: mảng `{r,c}`; `paints`: `{r,c,color}`; `lights`: `{r,c,phase}` (mặc định phase=0); `barriers`: `{r,c,need}`.
- `portals`: `{a:{r,c,side}, b:{r,c,side}}`; cạnh ô là chữ hoa `N|W|S|E`, khác cạnh bãi trong gates.
- `pylons`: `{r,c,o,fixed,to:[{r,c,side},{r,c,side}]}`. Nguồn 31 màn dùng o=`h|v` (h=0, v=1 lần xoay); fixed mặc định false, giữ cả hai đầu nối đúng thứ tự.
- `switches`: `{r,c,a:[{r,c}],b:[{r,c}]}`. `splash` mặc định false. Các danh sách thiếu ở nguồn được ghi `[]`.
- `minMoves`, `solveExhausted` lấy từ `levels-export`. **Màn 17–19: null → -1, solveExhausted=false**, vì BFS nguồn chưa kết luận trong 300000 trạng thái. Không dùng đường thắng đã biết để tự nhận là tối ưu. 28 màn còn lại được giải lại khi verify; maxStates=300000, đủ cho màn 16.
- Không chuyển `hint`, `tutorial`, `tag`, `explored` hoặc thời gian đo; đây không phải dữ liệu UI của bản cuối. Không dùng tọa độ mảng lồng nhau trong JSON level, để `JsonUtility` nạp được.

`solveExhausted=true` theo API prototype nghĩa là **đã có kết luận** (tìm được lời giải hoặc duyệt hết), không phải bị hết ngân sách. Với 28 màn hiện tại, lời giải ít lượt nhất đã được tìm và phát lại đến thắng. Màn -1 chỉ được kiểm nạp/luật/trace, không có kiểm chứng tối ưu.

## Trace JSON (`traces/trace_NN.json`)

Mỗi file có `schemaVersion:2`, `id`, `baseSeed`, `maxSteps:25`, `initialState`, `sequences`. Giữ **8 chuỗi random/màn** của 005, bắt đầu lại từ cùng trạng thái đầu; mỗi chuỗi có `kind:"random"`, `seed` riêng và `moves` tối đa 25 phần tử. Dừng sớm khi thắng hoặc không còn lệnh hợp lệ. Theo review 005, thêm **1 chuỗi `kind:"solution"`, seed=0** cho mỗi màn đã có minMoves (28 màn), lấy đường đi ít lượt nhất bằng CW.solve maxStates=300000; không cắt chuỗi lời giải theo giới hạn random. Màn 17–19 vẫn chỉ có 8 chuỗi random. Tổng 276 chuỗi; ngân sách dữ liệu ~≤3,6 MB, trace vẫn compact và đủ trường state.

Một phần tử `moves`:

```json
{"kind":"move","car":0,"sign":1,"moved":true,"exited":false,"events":[],"state":{"cars":[],"muds":[],"pyl":[],"moves":1}}
```

Ví dụ chỉ minh họa tên trường, không phải một bước chơi cụ thể.

- `kind`: `move|rot|pyl`; `car` là chỉ số xe cho move/rot, **chỉ số trụ cho pyl** (đúng tuple `['pyl', k, 0]` trong nguồn). `sign` ±1 cho move, 0 cho rot/pyl. Các chỉ số đều bắt đầu từ 0.
- `moved`, `exited` luôn ghi boolean; prototype không trả exited khi xoay thì ghi false. Vì chọn từ `nextMoves`, trace chỉ có lệnh hợp lệ/moved=true; test no-op/bị chặn vẫn thuộc EditMode 003–004.
- `events` giữ thứ tự và mọi trường nguồn: loại nằm ở `t`, gate là object `{side,i,color}` hoặc null. Push giữ `cars:[{j,r,c,fx}]`, sự kiện trong fx dùng **`k`**, không phải t. Tele giữ đủ `fr,fc,fside,tr,tc,tside,r,c,axis,sign`; Rotate/Pyl giữ trường nguồn.
- `state`: cars có `r,c,axis,color,mud,alive`; muds là bool[], pyl là số lần xoay 0..3, moves là tổng lượt. Giữ cả dữ liệu của xe đã rời bãi để so đúng từng bước.

PRNG xorshift32, base seed `0x20261122`. Seed chuỗi = `(baseSeed XOR imul(id,0x9e3779b9) XOR imul(sequence+1,0x85ebca6b)) >>> 0`, 0 được đổi thành 1; sequence bắt đầu từ 0. Chọn `floor(random * choices.length)` trong thứ tự `CW.nextMoves`. Không dùng `Math.random`, giờ máy hoặc đo thời gian trong dữ liệu sinh; JSON UTF-8 không BOM, kết thúc bằng LF.

## Verify kiểm những gì

1. Đủ đúng 31 level + 31 trace; so trường JSON với nguồn/metadata.
2. Chuyển JSON object về dữ liệu prototype, compile và so toàn bộ dữ liệu luật, kể cả Set/vật cản/đầu nối.
3. Chạy solve cho 28 màn có minMoves, so số lượt/solveExhausted và phát lại lời giải đến thắng. Màn 17–19 giữ chưa biết.
4. Tái tạo trace cùng seed từ **nguồn gốc**, rồi phát lại từng lệnh trên dữ liệu **đã round-trip qua JSON**; so command, moved/exited, mọi sự kiện và state, kiểm state đầu vào không bị sửa.
5. Tổng thư mục ≤3,6 MB theo review 005 cho phần bổ sung ở 006. Chạy lại generator phải cho byte y hệt; kết quả thực tế/hash kiểm lại được ghi ở phiếu tương ứng.

## Bộ nạp C# và kiểm EditMode (006)

`SqueakyLane.Game.LevelJsonLoader.Load(json, sourceName)` nhận chuỗi JSON và tên nguồn để báo lỗi, trả `LevelDef` bất biến; không tự đọc file hoặc Resources. Các DTO nằm cùng file loader. `sourceName` nên là tên màn/file, ví dụ `level_01.json`; dữ liệu sai ném `FormatException` có tên nguồn và nguyên nhân.

Mảng tùy chọn vắng/null trở thành mảng rỗng; mud/phase/fixed/truck/splash vắng nhận 0/false. `cars[].len` là trường bắt buộc của định dạng object (2 hoặc 3); loader không diễn giải len=0 thành 2. Khóa màu/trục/cạnh được kiểm theo định dạng trên, cùng kiểm tọa độ, thân xe/chồng xe và dữ liệu cơ chế.

`GoldenTraceTests` đọc `docs/golden` qua `Application.dataPath/..` trong Editor, dùng `JsonUtility` rồi gọi Core và so từng trường moved/exited/events/state; không đưa đường dẫn máy vào runtime. Seed không cần deserialize để replay. Root Pyl.k là số, Push.fx.k là chữ, nên dùng DTO riêng cho fx. Cổng null/DTO inline rỗng được so là không có cổng; cổng có dữ liệu vẫn so đầy đủ side/i/color.

Đường solution bổ sung vẫn chưa tạo fx không rỗng trong corpus này. Test `PushEffects_JsonKeysAndOrderMatchPrototypeFixture` dùng kết quả `CW.move` của một màn nhỏ có sơn → bùn → xịt để kiểm riêng JsonUtility đọc đủ Push.fx theo đúng thứ tự; không sửa trace chuẩn để ép Core khớp.

Kết quả kiểm C# nằm ở phiếu 006; việc 007 mới so solver C#. Không có bước Unity/Play/Android trong việc 005.
