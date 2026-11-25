using System;
using System.Collections.Generic;
using System.Linq;
using EWS_NSK_WPF.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace EWS_NSK_WPF.Services.ReportTemplates
{
    /// <summary>
    /// Шаблон "Список неисправных пожарных гидрантов и водоемов"
    /// По образцу документов ПСЧ-37 за 2022-2024 годы
    /// </summary>
    public class DefectiveHydrantsTemplate : IReportTemplate
    {
        public string TemplateName => "Список неисправных ПГ";

        public void Generate(List<Ewss> hydrants, string outputPath, Dictionary<string, object> parameters)
        {
            using var doc = DocX.Create(outputPath);

            // Извлекаем параметры
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

            // 2. ГРУППИРОВКА ПО ТИПАМ
            var grouped = hydrants.GroupBy(h => ClassifyOwner(h.OwnerName));

            int globalNumber = 1;

            foreach (var group in grouped.OrderBy(g => g.Key))
            {// Заголовок секции
                doc.InsertParagraph(group.Key)
                    .FontSize(11)
                    .Bold()
                    .UnderlineStyle(UnderlineStyle.singleLine);

                // Таблица
                var table = doc.AddTable(group.Count() + 1, 10);
                table.Design = TableDesign.TableGrid;

                // Заголовки колонок
                string[] headers = { "№", "Наименование населенного пункта", "Адрес привязки",
                    "Наименование предприятия", "Количество ПГ", "Диаметр водовода",
                    "Количество ПВ", "Емкость водоема", "Дата обнаружения неисправности",
                    "Отметка об устранении неисправности" };

                for (int col = 0; col < headers.Length; col++)
                    table.Rows[0].Cells[col].Paragraphs[0].Append(headers[col]).Bold().FontSize(9);

                // Данные
                int row = 1;
                foreach (var h in group)
                {
                    table.Rows[row].Cells[0].Paragraphs[0].Append(globalNumber.ToString()).FontSize(9);
                    table.Rows[row].Cells[1].Paragraphs[0].Append(h.AddressObjectAdm ?? "").FontSize(9);
                    table.Rows[row].Cells[2].Paragraphs[0].Append($"{h.AddressObjectName} {h.EwsHouseNumber}".Trim()).FontSize(9);
                    table.Rows[row].Cells[3].Paragraphs[0].Append(h.OwnerName ?? "").FontSize(9);
                    table.Rows[row].Cells[4].Paragraphs[0].Append("1").FontSize(9);
                    table.Rows[row].Cells[5].Paragraphs[0].Append(h.EwsDiameterWsCod > 0 ? $"К-{h.EwsDiameterWsCod}" : "").FontSize(9);
                    table.Rows[row].Cells[6].Paragraphs[0].Append("").FontSize(9);
                    table.Rows[row].Cells[7].Paragraphs[0].Append("").FontSize(9);
                    table.Rows[row].Cells[8].Paragraphs[0].Append(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(9);
                    table.Rows[row].Cells[9].Paragraphs[0].Append("").FontSize(9);
                    row++;
                    globalNumber++;
                }

                doc.InsertTable(table);
                doc.InsertParagraph("\n");
            }

            // 3. ПОДПИСЬ
            doc.InsertParagraph("Составил:").FontSize(11);
            doc.InsertParagraph(authorPosition).FontSize(11);
            doc.InsertParagraph(authorName).FontSize(11);
            doc.InsertParagraph("\n\n");

            // 4. СВОДНАЯ ТАБЛИЦА
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

        /// <summary>
        /// Классифицирует балансодержателя по категориям
        /// </summary>
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