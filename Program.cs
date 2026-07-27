using System.Diagnostics;
using Microsoft.Data.Sqlite;
using System.Linq;

string dbPath = "Data Source=takip.db";

string SureyiFormatla(int toplamSaniye)
{
    if (toplamSaniye < 60)
    {
        return toplamSaniye + " saniye";
    }

    int gun = toplamSaniye / 86400;
    int kalanSaniye = toplamSaniye % 86400;
    int saat = kalanSaniye / 3600;
    kalanSaniye = kalanSaniye % 3600;
    int dakika = kalanSaniye / 60;

    if (gun > 0)
    {
        return gun + " gün " + saat + " saat";
    }
    else if (saat > 0)
    {
        return saat + " saat " + dakika + " dakika";
    }
    else
    {
        return dakika + " dakika";
    }
}

using (var connection = new SqliteConnection(dbPath))
{
    connection.Open();

    var createLogTable = connection.CreateCommand();
    createLogTable.CommandText = @"
        CREATE TABLE IF NOT EXISTS activity_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            app_name TEXT NOT NULL,
            captured_at TEXT NOT NULL
        );
    ";
    createLogTable.ExecuteNonQuery();

    var createSessionTable = connection.CreateCommand();
    createSessionTable.CommandText = @"
        CREATE TABLE IF NOT EXISTS session (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            app_name TEXT NOT NULL,
            start_at TEXT NOT NULL,
            end_at TEXT NOT NULL,
            duration_sec INTEGER NOT NULL
        );
    ";
    createSessionTable.ExecuteNonQuery();

    var createCategoryTable = connection.CreateCommand();
    createCategoryTable.CommandText = @"
        CREATE TABLE IF NOT EXISTS category (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE
        );
    ";
    createCategoryTable.ExecuteNonQuery();

    var createAppTable = connection.CreateCommand();
    createAppTable.CommandText = @"
        CREATE TABLE IF NOT EXISTS app (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            category_id INTEGER REFERENCES category(id)
        );
    ";
    createAppTable.ExecuteNonQuery();

    var seedCategories = connection.CreateCommand();
    seedCategories.CommandText = @"
        INSERT OR IGNORE INTO category (name) VALUES ('İş');
        INSERT OR IGNORE INTO category (name) VALUES ('Sosyal Medya');
        INSERT OR IGNORE INTO category (name) VALUES ('Eğlence');
        INSERT OR IGNORE INTO category (name) VALUES ('Diğer');
    ";
    seedCategories.ExecuteNonQuery();

    var seedApps = connection.CreateCommand();
    seedApps.CommandText = @"
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Microsoft Word', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Microsoft Excel', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Microsoft PowerPoint', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('MSTeams', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Notion', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Evernote', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Slack', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('zoom.us', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Trello', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Asana', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Todoist', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Adobe Acrobat', 1);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Code', 1);

        INSERT OR IGNORE INTO app (name, category_id) VALUES ('WhatsApp', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Telegram', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Discord', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Instagram', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Facebook', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('X', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Twitter', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('LinkedIn', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Pinterest', 2);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Snapchat', 2);

        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Spotify', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Steam', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Epic Games Launcher', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('YouTube', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Netflix', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Disney+', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Amazon Prime Video', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Twitch', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('Apple Music', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('SoundCloud', 3);
        INSERT OR IGNORE INTO app (name, category_id) VALUES ('TikTok', 3);
    ";
    seedApps.ExecuteNonQuery();
}

Console.WriteLine("Veritabanı ve tablolar hazır.");

void OturumlaraCevir()
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    var selectCmd = connection.CreateCommand();
    selectCmd.CommandText = "SELECT app_name, captured_at FROM activity_log ORDER BY captured_at;";

    var reader = selectCmd.ExecuteReader();

    string? currentApp = null;
    DateTime sessionStart = DateTime.MinValue;
    DateTime lastSeen = DateTime.MinValue;

    var sessionsToInsert = new List<(string app, DateTime start, DateTime end)>();

    while (reader.Read())
    {
        string app = reader.GetString(0);
        DateTime capturedAt = DateTime.Parse(reader.GetString(1)).ToUniversalTime();

        if (currentApp == null)
        {
            currentApp = app;
            sessionStart = capturedAt;
            lastSeen = capturedAt;
        }
        else if (app == currentApp && (capturedAt - lastSeen).TotalMinutes <= 5)
        {
            lastSeen = capturedAt;
        }
        
        else
        {
            sessionsToInsert.Add((currentApp, sessionStart, lastSeen));

            double bosluk = (capturedAt - lastSeen).TotalMinutes;
            if (bosluk > 5)
            {
                sessionsToInsert.Add(("Bilgisayar boşta", lastSeen, capturedAt));
            }

            currentApp = app;
            sessionStart = capturedAt;
            lastSeen = capturedAt;
        }

    }

    if (currentApp != null)
    {
        sessionsToInsert.Add((currentApp, sessionStart, lastSeen));
    }

    reader.Close();

    var deleteCmd = connection.CreateCommand();
    deleteCmd.CommandText = "DELETE FROM session;";
    deleteCmd.ExecuteNonQuery();

    foreach (var s in sessionsToInsert)
    {
        var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO session (app_name, start_at, end_at, duration_sec)
            VALUES ($app, $start, $end, $duration);
        ";
        insertCmd.Parameters.AddWithValue("$app", s.app);
        insertCmd.Parameters.AddWithValue("$start", s.start.ToString("o"));
        insertCmd.Parameters.AddWithValue("$end", s.end.ToString("o"));
        insertCmd.Parameters.AddWithValue("$duration", (int)(s.end - s.start).TotalSeconds);
        insertCmd.ExecuteNonQuery();
    }

    Console.WriteLine(sessionsToInsert.Count + " oturum oluşturuldu.");
}

void GunlukOzetGoster()
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    var toplamCmd = connection.CreateCommand();
    toplamCmd.CommandText = @"
        SELECT SUM(duration_sec)
        FROM session
        WHERE date(start_at) = date('now');
    ";
    var toplamSonuc = toplamCmd.ExecuteScalar();
    int toplamSaniye = (toplamSonuc == DBNull.Value || toplamSonuc == null) ? 0 : Convert.ToInt32(toplamSonuc);

    Console.WriteLine("====================================");
    Console.WriteLine("BUGÜNKÜ ÖZET");
    Console.WriteLine("Toplam süre: " + SureyiFormatla(toplamSaniye));
    Console.WriteLine();
    Console.WriteLine("En çok kullanılan 5 uygulama:");

    var top5Cmd = connection.CreateCommand();
    top5Cmd.CommandText = @"
        SELECT app_name, SUM(duration_sec) AS toplam
        FROM session
        WHERE date(start_at) = date('now')
        GROUP BY app_name
        ORDER BY toplam DESC
        LIMIT 5;
    ";
    var reader = top5Cmd.ExecuteReader();
    int sira = 1;
    while (reader.Read())
    {
        string app = reader.GetString(0);
        int saniye = reader.GetInt32(1);
        Console.WriteLine(sira + ". " + app + " - " + SureyiFormatla(saniye));
        sira++;
    }
    Console.WriteLine("====================================");
}

void KategoriRaporuGoster()
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        SELECT c.name AS kategori, SUM(s.duration_sec) AS toplam_saniye
        FROM session s
        JOIN app a ON a.name = s.app_name
        JOIN category c ON c.id = a.category_id
        WHERE date(s.start_at) = date('now')
        GROUP BY c.name
        ORDER BY toplam_saniye DESC;
    ";

    var reader = cmd.ExecuteReader();

    Console.WriteLine("--- Kategori Bazlı Kullanım Raporu ---");
    while (reader.Read())
    {
        string kategori = reader.GetString(0);
        int toplamSaniye = reader.GetInt32(1);
        Console.WriteLine(kategori + ": " + SureyiFormatla(toplamSaniye));
    }
    Console.WriteLine("--------------------------------------");
}

void UygulamaRaporuGoster()
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        SELECT app_name, SUM(duration_sec) AS toplam_saniye
        FROM session
        WHERE date(start_at) = date('now')
        GROUP BY app_name
        ORDER BY toplam_saniye DESC;
    ";

    var reader = cmd.ExecuteReader();

    Console.WriteLine("--- Uygulama Bazlı Kullanım Raporu ---");
    while (reader.Read())
    {
        string app = reader.GetString(0);
        int toplamSaniye = reader.GetInt32(1);
        Console.WriteLine(app + ": " + SureyiFormatla(toplamSaniye));
    }
    Console.WriteLine("--------------------------------------");
}

void SaatlikDagilimGoster()
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        SELECT strftime('%H', start_at, '+3 hours') AS saat,
               SUM(duration_sec) AS toplam_saniye
        FROM session
        WHERE date(start_at, '+3 hours') = date('now', '+3 hours')
        GROUP BY saat
        ORDER BY saat;
    ";

    var reader = cmd.ExecuteReader();

    Console.WriteLine("--- Saatlik Dağılım ---");
    while (reader.Read())
    {
        string saat = reader.GetString(0);
        int toplamSaniye = reader.GetInt32(1);
        Console.WriteLine(saat + ":00 - " + SureyiFormatla(toplamSaniye));
    }
    Console.WriteLine("------------------------");
}

void HtmlRaporuOlustur(string baslangic, string bitis, string baslik)
{
    using var connection = new SqliteConnection(dbPath);
    connection.Open();

    
    var kategoriCmd = connection.CreateCommand();
    kategoriCmd.CommandText = @"
        SELECT COALESCE(c.name, 'Diğer') AS kategori, SUM(s.duration_sec)
        FROM session s
        LEFT JOIN app a ON a.name = s.app_name
        LEFT JOIN category c ON c.id = a.category_id
        WHERE date(s.start_at, '+3 hours') >= date($baslangic)
          AND date(s.start_at, '+3 hours') <= date($bitis)
          AND s.app_name != 'Bilgisayar boşta'
        GROUP BY kategori
        ORDER BY SUM(s.duration_sec) DESC;
    ";
    kategoriCmd.Parameters.AddWithValue("$baslangic", baslangic);
    kategoriCmd.Parameters.AddWithValue("$bitis", bitis);

    var kategoriEtiketleri = new List<string>();
    var kategoriDegerleri = new List<int>();
    var kategoriReader = kategoriCmd.ExecuteReader();
    while (kategoriReader.Read())
    {
        kategoriEtiketleri.Add(kategoriReader.GetString(0));
        kategoriDegerleri.Add(kategoriReader.GetInt32(1));
    }
    kategoriReader.Close();

    
    bool tekGun = (baslangic == bitis);

    var zamanCmd = connection.CreateCommand();
    if (tekGun)
    {
        zamanCmd.CommandText = @"
            SELECT strftime('%H', start_at, '+3 hours') AS birim, SUM(duration_sec)
            FROM session
            WHERE date(start_at, '+3 hours') = date($baslangic)
              AND app_name != 'Bilgisayar boşta'
            GROUP BY birim
            ORDER BY birim;
        ";
        zamanCmd.Parameters.AddWithValue("$baslangic", baslangic);
    }
    else
    {
        zamanCmd.CommandText = @"
            SELECT date(start_at, '+3 hours') AS birim, SUM(duration_sec)
            FROM session
            WHERE date(start_at, '+3 hours') >= date($baslangic)
              AND date(start_at, '+3 hours') <= date($bitis)
              AND app_name != 'Bilgisayar boşta'
            GROUP BY birim
            ORDER BY birim;
        ";
        zamanCmd.Parameters.AddWithValue("$baslangic", baslangic);
        zamanCmd.Parameters.AddWithValue("$bitis", bitis);
    }

    var zamanEtiketleri = new List<string>();
    var zamanDegerleri = new List<int>();
    var zamanReader = zamanCmd.ExecuteReader();
    while (zamanReader.Read())
    {
        string etiket = tekGun ? zamanReader.GetString(0) + ":00" : zamanReader.GetString(0);
        zamanEtiketleri.Add(etiket);
        zamanDegerleri.Add(zamanReader.GetInt32(1));
    }
    zamanReader.Close();

    string kategoriEtiketleriJson = "[" + string.Join(",", kategoriEtiketleri.Select(e => "\"" + e + "\"")) + "]";
    string kategoriDegerleriJson = "[" + string.Join(",", kategoriDegerleri) + "]";
    string zamanEtiketleriJson = "[" + string.Join(",", zamanEtiketleri.Select(e => "\"" + e + "\"")) + "]";
    string zamanDegerleriJson = "[" + string.Join(",", zamanDegerleri) + "]";
    string zamanBaslik = tekGun ? "Saatlik Aktif Kullanım Dağılımı" : "Günlük Aktif Kullanım Dağılımı";

    string html = @"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <title>Aktif Uygulama Takip Raporu</title>
    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
    <style>
        body { font-family: Arial, sans-serif; background: #1e1e1e; color: #eee; padding: 30px; }
        h1, h2.baslik { text-align: center; }
        .chart-container { width: 600px; margin: 40px auto; background: #2a2a2a; padding: 20px; border-radius: 10px; }
    </style>
</head>
<body>
    <h1>" + baslik + @"</h1>

    <div class='chart-container'>
        <h2>Kategori Dağılımı (Aktif Kullanım)</h2>
        <canvas id='kategoriChart'></canvas>
    </div>

    <div class='chart-container'>
        <h2>" + zamanBaslik + @"</h2>
        <canvas id='zamanChart'></canvas>
    </div>

    <script>
        function sureyiFormatla(saniye) {
            if (saniye < 60) return Math.round(saniye) + ' saniye';
            var gun = Math.floor(saniye / 86400);
            var kalan = saniye % 86400;
            var saat = Math.floor(kalan / 3600);
            kalan = kalan % 3600;
            var dakika = Math.floor(kalan / 60);
            if (gun > 0) return gun + ' gün ' + saat + ' saat';
            if (saat > 0) return saat + ' saat ' + dakika + ' dakika';
            return dakika + ' dakika';
        }

        new Chart(document.getElementById('kategoriChart'), {
            type: 'pie',
            data: {
                labels: " + kategoriEtiketleriJson + @",
                datasets: [{
                    data: " + kategoriDegerleriJson + @",
                    backgroundColor: ['#4e79a7', '#f28e2b', '#e15759', '#76b7b2']
                }]
            },
            options: {
                plugins: {
                    tooltip: { callbacks: { label: c => c.label + ': ' + sureyiFormatla(c.raw) } }
                }
            }
        });

        new Chart(document.getElementById('zamanChart'), {
            type: 'bar',
            data: {
                labels: " + zamanEtiketleriJson + @",
                datasets: [{
                    label: 'Aktif Kullanım Süresi',
                    data: " + zamanDegerleriJson + @",
                    backgroundColor: '#9370DB'
                }]
            },
            options: {
                plugins: {
                    tooltip: { callbacks: { label: c => sureyiFormatla(c.raw) } }
                },
                scales: {
                    y: { ticks: { callback: v => sureyiFormatla(v) } }
                }
            }
        });
    </script>
</body>
</html>
";

    File.WriteAllText("rapor.html", html);
    Console.WriteLine("HTML raporu oluşturuldu: rapor.html");
}




OturumlaraCevir();
GunlukOzetGoster();
KategoriRaporuGoster();
UygulamaRaporuGoster();
SaatlikDagilimGoster();

string bugun = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd");
HtmlRaporuOlustur(bugun, bugun, "Bugünkü Kullanım Raporu");

Console.WriteLine();
Console.WriteLine("Hangi aralığı görmek istersin?");
Console.WriteLine("1) Bugün (varsayılan olarak zaten oluşturuldu)");
Console.WriteLine("2) Bu hafta");
Console.WriteLine("3) Bu ay");
Console.WriteLine("4) Özel tarih aralığı");
Console.WriteLine("5) Hayır, geç");
Console.Write("Seçimin: ");

string? secim = Console.ReadLine();

if (secim == "2")
{
    string haftaBaslangic = DateTime.UtcNow.AddHours(3).AddDays(-7).ToString("yyyy-MM-dd");
    HtmlRaporuOlustur(haftaBaslangic, bugun, "Son 7 Günlük Rapor");
}
else if (secim == "3")
{
    string ayBaslangic = DateTime.UtcNow.AddHours(3).AddMonths(-1).ToString("yyyy-MM-dd");
    HtmlRaporuOlustur(ayBaslangic, bugun, "Son 30 Günlük Rapor");
}


else if (secim == "4")
{
    while (true)
    {
        Console.Write("Başlangıç tarihi (YYYY-MM-DD, örn: 2026-07-01): ");
        string girilenBaslangic = Console.ReadLine() ?? "";
        Console.Write("Bitiş tarihi (YYYY-MM-DD, örn: 2026-07-23): ");
        string girilenBitis = Console.ReadLine() ?? "";

        
        bool baslangicGecerli = DateTime.TryParseExact(girilenBaslangic, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime baslangicTarih);
        bool bitisGecerli = DateTime.TryParseExact(girilenBitis, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime bitisTarih);

        
        if (!baslangicGecerli)
            baslangicGecerli = DateTime.TryParse(girilenBaslangic, out baslangicTarih);
        if (!bitisGecerli)
            bitisGecerli = DateTime.TryParse(girilenBitis, out bitisTarih);

        
        if (!baslangicGecerli || !bitisGecerli)
        {
            Console.WriteLine("\n[X] Geçersiz bir tarih girdiniz! Lütfen YYYY-MM-DD formatında tekrar giriniz (Örn: 2026-07-15).\n");
            continue; 
        }

        
        if (baslangicTarih > bitisTarih)
        {
            Console.WriteLine("\n[X] Başlangıç tarihi, bitiş tarihinden sonra olamaz! Lütfen tekrar giriniz.\n");
            continue; 
        }

        
        string ozelBaslangic = baslangicTarih.ToString("yyyy-MM-dd");
        string ozelBitis = bitisTarih.ToString("yyyy-MM-dd");

        HtmlRaporuOlustur(ozelBaslangic, ozelBitis, ozelBaslangic + " - " + ozelBitis + " Arası Rapor");
        break; 
    }
}

while (true)
{
    var psi = new ProcessStartInfo();
    psi.FileName = "osascript";
    psi.Arguments = "-e \"tell application \\\"System Events\\\" to get name of first application process whose frontmost is true\"";
    psi.RedirectStandardOutput = true;
    psi.UseShellExecute = false;

    Process process = Process.Start(psi);
    string output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();

    using (var connection = new SqliteConnection(dbPath))
    {
        connection.Open();

        var checkApp = connection.CreateCommand();
        checkApp.CommandText = "SELECT COUNT(*) FROM app WHERE name = $app;";
        checkApp.Parameters.AddWithValue("$app", output);
        long appExists = (long)checkApp.ExecuteScalar();

        if (appExists == 0)
        {
            var insertApp = connection.CreateCommand();
            insertApp.CommandText = @"
                INSERT INTO app (name, category_id)
                VALUES ($app, 4);
            ";
            insertApp.Parameters.AddWithValue("$app", output);
            insertApp.ExecuteNonQuery();
        }

        var insert = connection.CreateCommand();
        insert.CommandText = @"
            INSERT INTO activity_log (app_name, captured_at)
            VALUES ($app, $time);
        ";
        insert.Parameters.AddWithValue("$app", output);
        insert.Parameters.AddWithValue("$time", DateTime.UtcNow.ToString("o"));
        insert.ExecuteNonQuery();
    }

    Console.WriteLine("Kaydedildi: " + output + " - " + DateTime.Now);

    Thread.Sleep(2000);
}