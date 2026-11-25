using System;
using System.Collections.Generic;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.Services.ReportTemplates
{
    /// <summary>
    /// Фабрика генераторов отчётов.
    /// По типу отчёта возвращает соответствующий шаблон.
    /// </summary>
    public static class ReportGenerator
    {
        /// <summary>
        /// Получить шаблон по типу отчёта
        /// </summary>
        public static IReportTemplate GetTemplate(string reportType)
        {
            return reportType switch
            {
                "Defective" => new DefectiveHydrantsTemplate(),
                // "Analysis" => new AnalysisTemplate(), // добавим позже
                // "Reconstruction" => new ReconstructionTemplate(), // добавим позже
                _ => throw new ArgumentException($"Неизвестный тип отчёта: {reportType}")
            };
        }

        /// <summary>
        /// Получить список всех доступных шаблонов
        /// </summary>
        public static List<IReportTemplate> GetAllTemplates()
        {
            return new List<IReportTemplate>
            {
                new DefectiveHydrantsTemplate()
                // new AnalysisTemplate(), // добавим позже
            };
        }

        /// <summary>
        /// Сгенерировать отчёт
        /// </summary>
        public static void GenerateReport(string reportType, List<Ewss> hydrants,
            string outputPath, Dictionary<string, object> parameters)
        {
            var template = GetTemplate(reportType);
            template.Generate(hydrants, outputPath, parameters);
        }
    }
}