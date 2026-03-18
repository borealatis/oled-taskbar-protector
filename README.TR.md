# 🖤 OLED Dimmer

OLED monitörlerde görev çubuğu yanmasını önlemek için geliştirilmiş hafif bir Windows uygulaması.

Görev çubuğunun üzerine yarı saydam siyah bir katman ekler. Program kapandığında hiçbir iz bırakmaz.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![Framework](https://img.shields.io/badge/.NET-Framework%204.x-blueviolet)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Özellikler

- Karartma seviyesini slider ile ayarlayın (%0 ile %96 arası)
- Sistem tepsisinden tek tıkla açıp kapatın
- Mouse görev çubuğuna geldiğinde karartmayı otomatik kaldırın (isteğe bağlı)
- Windows ile birlikte otomatik başlatın (isteğe bağlı)
- Tüm ayarlar otomatik kaydedilir, program her açılışta son ayarlarla gelir
- Sistem dosyalarına dokunmaz, yalnızca kendi kullanıcı Registry alanını kullanır
- Program kapatılınca hiçbir iz kalmaz

---

## Kurulum

### Gereksinimler

- Windows 10 / 11
- .NET Framework 4.x (Windows'ta zaten yüklü gelir)

### Derleme

1. `OledDimmer.cs` ve `build.bat` dosyalarını aynı klasöre indirin
2. `build.bat` dosyasına **sağ tıklayın → Yönetici olarak çalıştır**
3. `OledDimmer.exe` oluşturulur ve otomatik başlar

---

## Kullanım

Program başladığında sistem tepsisinde (sağ altta) küçük bir simge belirir.

| İşlem | Açıklama |
|---|---|
| Çift tıkla | Ayarlar panelini aç |
| Sağ tıkla | Karartmayı aç/kapat veya çıkış |

### Ayarlar Paneli

- **Karartma Seviyesi** — Slider ile istediğiniz koyuluğu seçin, anlık önizleme gösterilir
- **Karartmayı Kapat / Aç** — Geçici olarak devre dışı bırakın
- **Mouse gelince karartmayı kaldır** — Mouse görev çubuğuna girince overlay kalkar, ayrılınca geri gelir
- **Windows başlarken otomatik aç** — Bilgisayar açıldığında program otomatik çalışır
- **Programı Kapat** — Programı tamamen kapatır, overlay kalkar

---

## Teknik Detaylar

Overlay penceresi, `SetParent` API'si ile doğrudan görev çubuğunun child window'u olarak eklenir. Bu sayede:

- Ayrı bir z-order yarışı yoktur
- Tıklamalar overlay'i etkilemez, görev çubuğu normal çalışır
- Ekstra CPU/RAM kullanımı minimumdur (boşta %0 CPU, ~8 MB RAM)

Ayarlar `HKEY_CURRENT_USER\SOFTWARE\OledDimmer` altına kaydedilir. Sistem dosyalarına ve Windows ayarlarına dokunulmaz.

---

## Lisans

MIT License — istediğiniz gibi kullanabilir, değiştirebilir ve dağıtabilirsiniz.
