using Xunit;

public class FormatlamaTestleri
{
    [Fact]
    public void Saniye_60danKucukse_SaniyeFormatindaDonmeli()
    {
        int testSaniyesi = 45;
        string beklenen = "45 saniye";

        string sonuc = Yardimci.SureyiFormatla(testSaniyesi);

        Assert.Equal(beklenen, sonuc);
    }

    [Fact]
    public void Saniye_DakikaMertebesindeyse_DakikaFormatindaDonmeli()
    {
        int testSaniyesi = 150;
        string beklenen = "2 dakika";

        string sonuc = Yardimci.SureyiFormatla(testSaniyesi);

        Assert.Equal(beklenen, sonuc);
    }

    [Fact]
    public void Saniye_SaatMertebesindeyse_SaatVeDakikaDonmeli()
    {
        int testSaniyesi = 3660;
        string beklenen = "1 saat 1 dakika";

        string sonuc = Yardimci.SureyiFormatla(testSaniyesi);

        Assert.Equal(beklenen, sonuc);
    }
}