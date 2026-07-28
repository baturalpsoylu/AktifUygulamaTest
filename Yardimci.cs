public static class Yardimci
{
    public static string SureyiFormatla(int toplamSaniye)
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
}