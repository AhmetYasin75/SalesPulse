using System.Globalization;
using System.Text;
using SalesPulse.Application.Models;

namespace SalesPulse.Application.Services;

/// <summary>Sipariş satırlarını Türkçe Excel ile uyumlu CSV baytlarına çevirir.</summary>
public static class OrderCsvBuilder
{
    private const char Separator = ';';
    private static readonly CultureInfo Tr = new("tr-TR");

    private static readonly string[] Headers =
        { "Sipariş No", "Müşteri", "Şehir", "Tarih", "Tutar", "Ödeme Yöntemi", "Durum" };

    /// <summary>UTF-8 (BOM'lu) CSV içeriğini döndürür.</summary>
    public static byte[] Build(IEnumerable<OrderListItemDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator, Headers));

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(Separator, new[]
            {
                Escape(r.SiparisNo.ToString(CultureInfo.InvariantCulture)),
                Escape(r.Musteri),
                Escape(r.Sehir),
                Escape(r.Tarih.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                Escape(r.Tutar.ToString("0.00", Tr)),
                Escape(r.OdemeYontemi),
                Escape(r.Durum)
            }));
        }

        var preamble = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());

        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    /// <summary>Formül enjeksiyonunu engeller; ayraç, tırnak ve satır sonu içeren alanları tırnaklar.</summary>
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Excel hücreyi formül sanmasın (=, +, -, @ ile başlayanlar)
        if (value[0] is '=' or '+' or '-' or '@')
            value = "'" + value;

        if (value.IndexOfAny(new[] { Separator, '"', '\r', '\n' }) >= 0)
            value = "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}