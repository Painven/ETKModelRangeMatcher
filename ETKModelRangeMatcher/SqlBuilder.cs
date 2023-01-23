using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ETKModelRangeMatcher;

public static class SqlBuilder
{
    public static string GetModelRangesDescriptions(Dictionary<int, int[]> modelRangeInfo, IEnumerable<ProductData> products)
    {
        var sb = new StringBuilder();

        sb
            .AppendLine("UPDATE oc_product_description")
            .AppendLine("SET description = case product_id");
        string pidArray = string.Join(",", modelRangeInfo.Keys);
        foreach (var kvp in modelRangeInfo)
        {
            var adjacent_info = products.Where(p => kvp.Value.Contains(p.ProductId)).ToArray();

            string descriptionHtmlEncoded = GetDescription(adjacent_info);
            sb.AppendLine($"WHEN '{kvp.Key}' THEN '{descriptionHtmlEncoded}'");
        }

        sb.AppendLine("ELSE ''");
        sb.AppendLine("END");
        sb.Append($"WHERE product_id IN ({pidArray});");

        var result = sb.ToString();
        return result;
    }

    private static string GetDescription(IEnumerable<ProductData> adjacentProducts)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<h2>Модельный ряд</h2>");
        sb.AppendLine("<div class=\"table-responsive empty-model-range-data-table\">");
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<tbody>");

        sb.AppendLine($"<tr><th>Артикул</th><th>Наименование</th></tr>");

        foreach (var product in adjacentProducts)
        {
            sb.AppendLine($"<tr><td>{product.Sku ?? "-"}</td><td>{HttpUtility.HtmlDecode(product.Name)}</td></tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.Append("</div>");

        var result = HttpUtility.HtmlEncode(sb.ToString());
        return result;
    }
}

public class ProductData
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
}
