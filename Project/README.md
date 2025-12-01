# XiaoZhi Unity Project

Ứng dụng trợ lý/đồng hành xây bằng **Unity 2022.1.24f1**. Kết hợp đánh thức bằng từ khóa, chat/hiển thị animation (VRM hoặc emoji), phát nền/wallpaper, điều khiển nhà thông minh MIoT và đa ngôn ngữ (Anh/Việt).

## Các hệ thống chính
- **Điểm khởi chạy (`Assets/Scripts/App.cs`)**: Quy trình boot, kiểm tra quyền, OTA, chuẩn bị tài nguyên wake-word, khởi tạo audio (codec/resampler Opus), khởi tạo protocol và vòng lặp chính.
- **Lớp hiển thị (`Assets/Scripts/Display`)**: Hai chế độ:
  - `VRMDisplay` / `EmojiDisplay` tải avatar/nền (`WallpaperUI`), gắn trạng thái/chat vào `MainUI`, hỗ trợ phát animation/nhảy/video.
  - `WallpaperUI` cập nhật nền sprite/video/gif từ preset; lắng nghe `AppSettings.Instance.OnWallPaperUpdate`.
  - **EmojiMainUI** xử lý 6 trạng thái emoji: `sleep`, `neutral`, `happy`, `funny`, `sad`, `thinking` (map ra 😴 🙂 😄 😜 🙁 🤔); có hiệu ứng “breathing” khi Idle/Connecting và hiệu ứng “wake-up” khi chuyển từ Connecting sang Listening; hiển thị phổ âm thanh vào/ra.
- **UI**:
  - `SettingsUI` (prefab `Assets/Res/UI/SettingsUI/SettingsUI.prefab`) chứa tab App và MIoT.
  - `AppSettingsUI` hiển thị ngôn ngữ, display/zoom/break mode, danh sách wallpaper, volume, theme, URL/token, keywords.
  - `MIoTSettingsUI` và `MIoTDeviceInfo` quản lý thiết bị Xiaomi IoT.
- **Audio**:
  - `AudioCodec`, `OpusEncoder/Decoder/Resampler`, `AudioClipStreamReader` xử lý thu/phát âm thanh.
  - Wake-word dùng model sherpa-onnx khai báo trong `AppPresets` (`_keyWords`) và copy từ StreamingAssets khi chạy.
- **Chat/animation**:
  - State machine `Talk` điều khiển animation qua `AnimationCtrl`; từ khóa tạm biệt trong `AnimationCtrl.ByeKeywords`.
  - `ThingAnimation`, `ThingDance`, `ThingVideoPlayer` cung cấp API điều khiển qua hệ thống Thing.
- **Hệ thống Thing (`Assets/Scripts/IoT`)**: Trừu tượng thiết bị; `ThingManager` nạp Things; các Thing MIoT (animation/nhảy/app settings/video player/nền tảng miot).
- **Tích hợp MIoT (`Assets/Scripts/MIoT`)**:
  - `MiotCommand`/`MiotSpec` lấy spec, thiết bị, phòng; `MiotTranslation` nạp bảng dịch.
  - `ThingMIoT` đăng ký getter/setter/action cho thiết bị theo dõi; lưu cấu hình trong `Settings/miot`.
- **Đa ngôn ngữ**:
  - Unity Localization với locale English (`Assets/Settings/Localization/English (en).asset`) và Vietnamese (`Assets/Settings/Localization/Vietnamese (vi).asset`).
  - Bảng chuỗi `Lang_en.asset` / `Lang_vi.asset` nối qua `Lang.asset` CSV (`Assets/Settings/Localization/Lang.csv`).
  - Addressables: `Localization-Locales`, `Localization-String-Tables-English`, `Localization-String-Tables-vi`.
- **Preset & cài đặt**:
  - `AppPresets.asset`: WebSocket URL, cấu hình wake-word, model VAD, wallpapers (`_wallpapers`), nhân vật VRM, video, animation.
  - `AppSettings` (PlayerPrefs): display mode, zoom, break mode, volume, theme, wallpaper, URL/token, auto-hide UI, ngôn ngữ, MAC override.

## Cấu trúc thư mục chính
- `Assets/Scripts`: Logic lõi (App, audio, display, UI, IoT/MIoT, protocol, utils).
- `Assets/Res`: Prefab, background, video, VRM cho UI/hiển thị.
- `Assets/Settings`: Localization, presets, MIoT translations, keystore, thiết lập Addressables.
- `Assets/Plugins`: FMOD, UniGLTF/VRM10, sherpa-onnx, v.v.
- `Assets/StreamingAssets`: Model/keyword wake-word đóng gói để copy runtime.

## Build & Run
1. Phiên bản Unity: **2022.1.24f1**.
2. Mở project, Unity tự khôi phục gói (UniTask, Localization, Addressables, DOTween, FMOD, UniGLTF/VRM10).
3. Addressables: Build content nếu thay đổi (`Window > Asset Management > Addressables > Build > New Build > Default Build Script`).
4. Nền tảng:
   - **PC**: Chạy trong Editor sau khi cấp quyền mic/camera.
   - **Android**: Đảm bảo keystore (`Project/user.keystore`) và Player Settings hợp lệ; đã bao gồm FMOD libs.
5. Scene khởi chạy: `Assets/Scenes/Entry.unity` (đã thêm vào `EditorBuildSettings.asset`).

## Ghi chú sử dụng
- **Wallpaper**: Thêm entry vào `_wallpapers` trong `AppPresets.asset` với loại (`Default/Sprite/Video/Gif`), tên, và đường dẫn asset (vd `Assets/Res/Background/your_image.png` hoặc `Assets/Res/Video/your_video.mp4`). Xuất hiện trong Settings → Wallpaper.
- **Localization**: Thêm chuỗi vào `Assets/Settings/Localization/Lang.csv` và rebuild bảng; thêm locale asset + label Addressables nếu có ngôn ngữ mới.
- **MIoT**: Cấu hình danh sách thiết bị theo dõi trong `ThingMIoT` (key `watch_device_dids`). Dữ liệu dịch trong `Assets/Settings/MIoT/translation_languages.json`.
- **Voice**: Model/keyword wake-word nằm ở `StreamingAssets/sherpa-onnx/...`; điều chỉnh `_keyWords` trong `AppPresets.asset` theo locale.

## Phụ thuộc
- Cysharp UniTask, Unity Addressables, Unity Localization
- DOTween (DG.Tweening) cho UI animation
- Sherpa-ONNX cho wake-word/VAD
- FMOD cho audio playback
- UniGLTF/VRM10 cho hỗ trợ avatar VRM

## Lưu ý repository
- Các thư mục sinh ra (Library/Temp/Logs) không commit. Output Addressables nên build lại trên máy đích. Keystore kèm repo chỉ dùng khi bạn chủ động xây dựng gói phát hành.
