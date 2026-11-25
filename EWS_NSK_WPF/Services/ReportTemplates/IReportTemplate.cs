using System.Collections.Generic;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.Services.ReportTemplates
{
    /// <summary>
    /// Интерфейс шаблона отчёта.
    /// Каждый тип отчёта (неисправные, анализ, реконструкция) реализует этот интерфейс.
    /// </summary>
    public interface IReportTemplate
    {
        /// <summary>
        /// Название шаблона (отображается в UI)
        /// </summary>
        string TemplateName { get; }

        /// <summary>
        /// Генерирует документ Word по шаблону
        /// </summary>
        /// <param name="hydrants">Данные для отчёта</param>
        /// <param name="outputPath">Путь сохранения файла</param>
        /// <param name="parameters">Дополнительные параметры (территория, год, автор и т.д.)</param>
        void Generate(List<Ewss> hydrants, string outputPath, Dictionary<string, object> parameters);
    }
}