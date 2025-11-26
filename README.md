# xiaozhi-unity

Phiên bản Unity của dự án [xiaozhi-esp32](https://github.com/78/xiaozhi-esp32).

<div style="display: flex; justify-content: space-between;">
  <img src="Docs/Emoji.gif" width="360" />
  <img src="Docs/VRM.gif" width="360" />
</div>
<div style="display: flex; justify-content: space-between;">
  <img src="Docs/MIoT.png" width="360" />
</div>

## Chức năng chính

- Tích hợp MIoT, điều khiển thiết bị Mi Home bằng giọng nói
- Trò chuyện thoại với Tiểu Trí
- Chế độ biểu hiện:
  - Emoji biểu cảm
  - Mô hình VRM
- Chế độ ngắt lời thoại:
  - Từ khóa (từ khóa ngắt lời --> bước vào lượt thoại tiếp theo)
  - Giọng người (VAD ngắt lời --> trễ 1 giây --> lượt thoại tiếp theo)
  - Tự do (VAD ngắt lời --> không ngắt mạch hội thoại)
- Cấu hình được từ kích hoạt/từ khóa
- Hai chủ đề giao diện
- Hỗ trợ cấu hình tùy chỉnh

## Nền tảng hỗ trợ

| Nền tảng/kiến trúc | x64 | arm64 | arm32 |
|-----------|----|----|----|
| Windows   | ✅ | -- | -- |
| Linux     | ⚠️ | -- | -- |
| MacOS     | ⚠️ | ⚠️ | -- |
| Android   | -- | ✅ | ✅ |
| iOS       | -- | ⚠️ | -- |

✅ Đã hỗ trợ  
⚠️ Hỗ trợ nhưng chưa thử nghiệm  

## Cách triển khai

- Dùng FMOD để thu và phát âm thanh
- Tích hợp mô-đun WebRTC APM cho tiền xử lý âm thanh, hỗ trợ khử vọng, giảm ồn, tăng cường âm
- Tích hợp sherpa-onnx cho nhận dạng giọng nói thời gian thực, gồm VAD và Keyword Spot
- Tích hợp VRM1.0
- Dùng uLipSync để đồng bộ khẩu hình

## Công cụ

Cách build: chọn `Assets/Settings/BuildPresets.asset`, giao diện build sẽ hiện trong EditorView

Nhập VRM: sau khi import mô hình VRM vào Unity, tại cửa sổ Project hãy nhấp chuột phải vào mô hình và chọn menu `VRM10/PreProcess` để tiền xử lý, sau đó cấu hình VRM Character Model trong `Settings/AppPreset.asset`

*Lưu ý: `Settings/AppPreset.asset` về cơ bản đã chứa sẵn mọi cấu hình mẫu*

## Reference

- [xiaozhi-esp32](https://github.com/78/xiaozhi-esp32)
- [FMOD](https://github.com/fmod/fmod-for-unity)
- [webrtc audio processing](https://gitlab.freedesktop.org/pulseaudio/webrtc-audio-processing)
- [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)
- [uLipSync](https://github.com/hecomi/uLipSync)
- [UniVRM](https://github.com/vrm-c/UniVRM)
- [Nguồn mô hình](https://hub.vroid.com/en/characters/1245908975744054638/models/2140572620978697176)
- [MiService](https://github.com/Yonsm/MiService)
- [hass-xiaomi-miot](https://github.com/al-one/hass-xiaomi-miot)
