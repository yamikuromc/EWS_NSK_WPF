using System.Windows.Media;

namespace EWS_NSK_WPF.Helpers
{
    public static class MarkerColorHelper
    {
        /// <summary>
        /// Возвращает цвета маркера.
        /// 1. Приоритет: текст из справочника (гибкость для админа).
        /// 2. Страховка: числовой код из основной таблицы.
        /// </summary>
        public static (Color fill, Color stroke) GetColorsForStatus(int statusCode, string statusName)
        {
            if (!string.IsNullOrEmpty(statusName))
            {
                string s = statusName.ToLower().Trim();

                // ВАЖНО: сначала "неисправен" (иначе "исправен" сработает раньше)
                if (s.Contains("неисправен") || s.Contains("сломан") || s.Contains("авария"))
                    return (Color.FromRgb(244, 67, 54), Color.FromRgb(211, 47, 47)); // КРАСНЫЙ

                if (s.Contains("исправен") || s.Contains("работает") || s.Contains("норма"))
                    return (Color.FromRgb(33, 150, 243), Color.FromRgb(25, 118, 210)); // СИНИЙ

                // ЖЁЛТЫЙ вместо оранжевого — лучше видно и не путается с красным
                if (s.Contains("требует") || s.Contains("проверк"))
                    return (Color.FromRgb(255, 235, 59), Color.FromRgb(245, 215, 0)); // ЖЁЛТЫЙ

                if (s.Contains("демонтирован"))
                    return (Color.FromRgb(66, 66, 66), Colors.Black); // ЧЁРНЫЙ

                if (s.Contains("не найден"))
                    return (Color.FromRgb(100, 100, 100), Color.FromRgb(60, 60, 60)); // ТЁМНО-СЕРЫЙ
            }

            // Страховка по коду
            return statusCode switch
            {
                1 => (Color.FromRgb(33, 150, 243), Color.FromRgb(25, 118, 210)),  // Синий
                2 => (Color.FromRgb(244, 67, 54), Color.FromRgb(211, 47, 47)),   // Красный
                3 => (Color.FromRgb(255, 235, 59), Color.FromRgb(245, 215, 0)),  // Жёлтый
                4 => (Color.FromRgb(66, 66, 66), Colors.Black),                  // Чёрный
                5 => (Color.FromRgb(100, 100, 100), Color.FromRgb(60, 60, 60)),  // Тёмно-серый
                _ => (Color.FromRgb(158, 158, 158), Color.FromRgb(117, 117, 117)) // Серый
            };
        }
    }
}