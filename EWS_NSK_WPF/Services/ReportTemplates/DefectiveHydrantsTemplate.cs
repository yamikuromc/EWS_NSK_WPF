using System;
using System.Collections.Generic;
using System.Linq;
using EWS_NSK_WPF.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace EWS_NSK_WPF.Services.ReportTemplates
{
    public class DefectiveHydrantsTemplate : IReportTemplate
    {
        public string TemplateName => "Список неисправных ПГ";

        public void Generate(List<Ewss> hydrants, string outputPath, Dictionary<string, object> parameters)
        {
            using var doc = DocX.Create(outputPath);

            // Параметры
            string territory = parameters.TryGetValue("territory", out var t) ? t?.ToString() ?? "" : "";
            string season = parameters.TryGetValue("season", out var s) ? s?.ToString() ?? "" : "";
            int year = parameters.TryGetValue("year", out var y) && y is int yi ? yi : DateTime.Now.Year;
            string authorPosition = parameters.TryGetValue("authorPosition", out var ap) ? ap?.ToString() ?? "" : "";
            string authorName = parameters.TryGetValue("authorName", out var an) ? an?.ToString() ?? "" : "";

            // 1. ЗАГОЛОВОК
            doc.InsertParagraph("Список неисправных пожарных гидрантов и водоемов,")
                .FontSize(12).Bold().Alignment = Alignment.center;
            doc.InsertParagraph($"находящихся на территории {territory} по итогам {season} проверки {year} года")
                .FontSize(12).Bold().Alignment = Alignment.center;
            doc.InsertParagraph("\n");

            // 2. ГРУППИРОВКА
            var grouped = hydrants.GroupBy(h => ClassifyOwner(h.OwnerName));

            int globalNumber = 1;

            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                // Заголовок секции (Муниципальные / Ведомственные / Бесхозные)
                doc.InsertParagraph(group.Key)
                    .FontSize(11).Bold();

                // Нумерованный список
                int itemNumber = 1;
                foreach (var h in group)
                {
                    string address = $"{h.AddressObjectName} {h.EwsHouseNumber}".Trim();
                    string defect = h.EwsStateNote ?? "";

                    string line = $"{itemNumber}. {address} – {defect}";

                    var p = doc.InsertParagraph(line);
                    p.FontSize(11);
                    p.IndentationBefore = 720; // 720 twips ≈ 1.27 см (стандартный абзацный отступ как в Word)

                    itemNumber++;
                    globalNumber++;
                }

                doc.InsertParagraph("\n");
            }

            // 3. ПОДПИСЬ
            doc.InsertParagraph("Составил:").FontSize(11);
            doc.InsertParagraph(authorPosition).FontSize(11);
            doc.InsertParagraph(authorName).FontSize(11);
            doc.InsertParagraph("\n\n");

            // 4. СВОДНАЯ ТАБЛИЦА (она есть в образце 2024 года)
            var summaryTable = doc.AddTable(4, 3);
            summaryTable.Design = TableDesign.TableGrid;

            summaryTable.Rows[0].Cells[0].Paragraphs[0].Append("").Bold();
            summaryTable.Rows[0].Cells[1].Paragraphs[0].Append("Всего").Bold();
            summaryTable.Rows[0].Cells[2].Paragraphs[0].Append("Неисправны").Bold();

            var counts = new Dictionary<string, int>();
            foreach (var g in grouped) counts[g.Key] = g.Count();

            string[] categories = { "Муниципальные", "Ведомственные", "Бесхозные" };
            for (int i = 0; i < 3; i++)
            {
                int count = counts.TryGetValue(categories[i], out var c) ? c : 0;
                summaryTable.Rows[i + 1].Cells[0].Paragraphs[0].Append(categories[i]).FontSize(9);
                summaryTable.Rows[i + 1].Cells[1].Paragraphs[0].Append(count.ToString()).FontSize(9);
                summaryTable.Rows[i + 1].Cells[2].Paragraphs[0].Append(count.ToString()).FontSize(9);
            }

            doc.InsertTable(summaryTable);
            doc.Save();
        }

        private string ClassifyOwner(string ownerName)
        {
            if (string.IsNullOrEmpty(ownerName) || ownerName == "Бесхозный")
                return "Бесхозные";

            string lower = ownerName.ToLower();
            if (lower.Contains("муниципальн") || lower.Contains("муницип") ||
                lower.Contains("горводоканал") || lower.Contains("администрация"))
                return "Муниципальные";

            return "Ведомственные";
        }
    }
}