using System;
using System.Collections.Generic;
using System.Linq;
using EWS_NSK_WPF.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace EWS_NSK_WPF.Services
{
    public class WordReportService
    {
        public void GenerateFormalReport(List<Ewss> hydrants, string outputPath, string reportType, string userName)
        {
            using var doc = DocX.Create(outputPath);

            // 1. ШАПКА ДОКУМЕНТА
            doc.InsertParagraph("УТВЕРЖДАЮ").Alignment = Alignment.right;
            doc.InsertParagraph("Начальник подразделения ППС").Alignment = Alignment.right;
            doc.InsertParagraph($"«__» __________ 20{DateTime.Now.Year.ToString().Substring(2)} г.").Alignment = Alignment.right;
            doc.InsertParagraph("\n\n");

            // 2. ЗАГОЛОВОК
            string title = reportType switch
            {
                "Defective" => "ВЕДОМОСТЬ дефектов и неисправностей источников наружного противопожарного водоснабжения",
                "ByOwner" => "СПРАВКА о состоянии источников водоснабжения по балансодержателям",
                _ => "АКТ проверки состояния источников наружного противопожарного водоснабжения (ИНППВ)"
            };

            doc.InsertParagraph(title)
                .FontSize(14)
                .Bold()
                .Alignment = Alignment.center;
            doc.InsertParagraph("\n");

            // 3. ПРЕАМБУЛА (Автозаполнение!)
            doc.InsertParagraph($"Настоящий документ составлен «{DateTime.Now:dd}» {GetMonth(DateTime.Now.Month)} {DateTime.Now:yyyy} г. " +
                                $"в результате проверки, проведенной: {userName}.")
                .FontSize(11);
            doc.InsertParagraph($"Всего проверено гидрантов: {hydrants.Count}.")
                .FontSize(11);
            doc.InsertParagraph("\n");

            // 4. ТАБЛИЦА ДАННЫХ
            var table = doc.AddTable(hydrants.Count + 1, 5);
            table.Design = TableDesign.TableGrid;

            // Заголовки столбцов
            table.Rows[0].Cells[0].Paragraphs[0].Append("№ ПГ").Bold();
            table.Rows[0].Cells[1].Paragraphs[0].Append("Адрес").Bold();
            table.Rows[0].Cells[2].Paragraphs[0].Append("Статус").Bold();
            table.Rows[0].Cells[3].Paragraphs[0].Append("Балансодержатель").Bold();
            table.Rows[0].Cells[4].Paragraphs[0].Append("Примечание / Дефект").Bold();

            // Заполнение данными
            for (int i = 0; i < hydrants.Count; i++)
            {
                var h = hydrants[i];
                table.Rows[i + 1].Cells[0].Paragraphs[0].Append(h.EwsNumber.ToString());
                table.Rows[i + 1].Cells[1].Paragraphs[0].Append($"{h.AddressObjectName} {h.EwsHouseNumber}".Trim());
                table.Rows[i + 1].Cells[2].Paragraphs[0].Append(h.StatusName);
                table.Rows[i + 1].Cells[3].Paragraphs[0].Append(h.OwnerName);
                table.Rows[i + 1].Cells[4].Paragraphs[0].Append(h.EwsStateNote);
            }
            doc.InsertTable(table);

            // 5. ПОДВАЛ (Подписи)
            doc.InsertParagraph("\n\n");
            doc.InsertParagraph("Проверку провел: _________________ / " + userName + " /")
                .FontSize(11);

            doc.Save();
        }

        private string GetMonth(int month)
        {
            string[] months = { "", "января", "февраля", "марта", "апреля", "мая", "июня",
                                "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            return months[month];
        }
    }
}