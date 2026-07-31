# Aktif Uygulama Kullanım Süresi Takipçisi

macOS için Ekran Süresi (Screen Time) benzeri, arka planda çalışan bir masaüstü kullanım takip uygulaması.

## Kullanılan Teknolojiler

- **.NET 8 (C#)** — uygulama dili ve çalışma zamanı
- **SQLite** — veri depolama (dosya bazlı, kurulum gerektirmez)
- **Microsoft.Data.Sqlite (ADO.NET)** — veritabanı erişimi (ham SQL, Entity Framework Core kullanılmadı)
- **osascript (AppleScript)** — macOS'ta aktif uygulamayı tespit etmek için `Process.Start` ile çağrılır
- **Chart.js** — HTML raporunda kategori ve zaman dağılımı grafikleri
- **xUnit** — unit test framework'ü

## Nasıl Kurulur

1. [.NET 8 SDK](https://dotnet.microsoft.com/download) kurulu olmalı.
2. Bu depoyu klonla:
```
git clone https://github.com/baturalpsoylu/AktifUygulamaTest.git
cd AktifUygulamaTest
```
3. Gerekli paketi yüklemek için terminale yazın:
```
dotnet restore
```
4. macOS'ta **Sistem Ayarları → Gizlilik ve Güvenlik → Otomasyon** altında, kullandığın terminal uygulamasına **System Events** için izin ver. (İlk çalıştırmada sistem otomatik soracaktır.)

## Nasıl Çalıştırılır

```
dotnet run
```

Program çalıştığında arka planda her 2 saniyede bir aktif uygulamayı kaydetmeye başlar. Veri toplamayı durdurmak için **Ctrl+C**'ye bas. (`Ctrl+C`, macOS/Linux/Windows'ta terminal uygulamalarını durdurmak için evrensel olarak kullanılan, platformdan bağımsız bir kısayoldur.)

Ctrl+C'ye bastıktan sonra:
1. Toplanan ham kayıtlar oturumlara (session) dönüştürülür
2. Bugünkü özet, kategori raporu, uygulama raporu ve saatlik dağılım konsola yazdırılır
3. `rapor.html` otomatik oluşturulur (grafiklerle bugünkü raporu gösterir)
4. Bir menüden farklı tarih aralığı seçilebilir:
   - 1) Bugün
   - 2) Bu hafta
   - 3) Bu ay
   - 4) Özel tarih aralığı
   - 5) CSV olarak dışa aktar
   - 6) Hayır, geç

Menüde ve alt menülerde geçersiz bir değer girilirse (örneğin listede olmayan bir sayı ya da harf), sistem hatayı bildirip geçerli bir değer girilene kadar tekrar sorar; program çökmez veya beklenmeyen bir duruma geçmez.

Raporu tarayıcıda görmek için terminale yazın:
```
open rapor.html
```

## Proje Yapısı

- `Program.cs` — ana program: veri toplama, oturumlaştırma, raporlama
- `Yardimci.cs` — bağımsız yardımcı fonksiyonlar (örn. süre formatlama)
- `AktifUygulamaTest.Tests/` — unit test projesi

## Testler

Proje, veritabanına veya macOS'a bağımlı olmayan, saf mantık içeren fonksiyonlar (örn. süre formatlama) için unit testler içerir.

Testleri çalıştırmak için:
```
cd AktifUygulamaTest.Tests
dotnet test
```

## Veritabanı Şeması

- `activity_log` — ham ölçümler (uygulama adı + zaman damgası)
- `session` — ardışık ölçümlerden birleştirilmiş oturumlar (başlangıç, bitiş, süre)
- `category` — İş / Sosyal Medya / Eğlence / Diğer kategorileri
- `app` — bilinen uygulamalar ve kategorileri (bilinmeyen uygulamalar otomatik "Diğer"e atanır)

## Bonus Özellikler

**CSV Dışa Aktarma:** Menüden seçilen tarih aralığında (bugün / bu hafta / bu ay / özel aralık) iki ayrı CSV dosyası oluşturulur:

- `rapor_detay_BAŞLANGIÇ_BİTİŞ.csv` — her oturumun tek tek listesi (uygulama, kategori, başlangıç/bitiş zamanı — Türkiye saatiyle, dakika hassasiyetinde — ve süre)
- `rapor_ozet_BAŞLANGIÇ_BİTİŞ.csv` — "uygulamayı toplamda ne kadar kullandın" raporu: aynı uygulamanın aynı gün içindeki tüm oturumları toplanıp tek satırda gösterilir, en çok kullanılan uygulama en üstte listelenir

Her iki dosyada da:
- "Bilgisayar boşta" kayıtları dahil edilmez
- Süreler okunabilir formatta gösterilir (örn. "5 saniye", "13 dakika", "1 saat, 20 dakika", "1 gün, 12 saat")

## Girdi Doğrulama

- Ana menüde ve CSV alt menüsünde, kullanıcı geçerli aralıktaki bir sayı girene kadar sistem tekrar soru sorar.
- Özel tarih aralığı girişlerinde geçersiz format veya mantıksız aralık (başlangıç tarihi bitişten sonra) girildiğinde kullanıcı uyarılır ve tekrar doğru bir değer girene kadar döngüde tutulur.

## Bilinen Eksikler / Sınırlamalar

- Web tabanlı servisler (YouTube, Instagram, Netflix vb.) yalnızca **masaüstü uygulaması** olarak açıldığında doğru tespit edilir; tarayıcı sekmesinde açılırsa "Google Chrome" / "Safari" olarak görünür.
- Örnekleme aralığı 2 saniyedir; bu aralıktan daha kısa süren uygulama geçişleri (<2 saniye) yakalanamayabilir.
- Zaman verisi veritabanında UTC olarak saklanır, raporlama sırasında Türkiye saatine (+3 saat) çevrilir.