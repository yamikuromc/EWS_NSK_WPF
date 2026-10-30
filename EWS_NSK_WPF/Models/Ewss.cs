namespace EWS_NSK_WPF.Models
{
    public class Ewss
    {
        // Основные поля из tbl_EWS
        public int EwsUnitCod { get; set; }
        public int EwsMapId { get; set; }
        public int EwsTypeCod { get; set; }
        public int EwsNumber { get; set; }
        public string EwsName { get; set; } = "";
        public int EwsStatusCod { get; set; }
        public string EwsTroubleEwsCod { get; set; } = "";
        public int EwsPipeTypeCod { get; set; }
        public int EwsDiameterWsCod { get; set; }
        public int EwsDiameterPkCod { get; set; }
        public int EwsCountPa { get; set; }
        public int EwsValueCod { get; set; }
        public int EwsOwnerCod { get; set; }

        // Координаты (в БД TEXT, но мы храним как double для удобства)
        public double LatitudeD { get; set; }
        public double LongitudeD { get; set; }

        // Состояние и адрес
        public string EwsState { get; set; } = "";
        public string EwsStateNote { get; set; } = "";
        public string EwsAddressObjectCod { get; set; } = "";
        public string EwsHouseNumber { get; set; } = "";

        // Привязка
        public string EwsPriviazka { get; set; } = "";
        public int EwsPrLeft { get; set; }
        public int EwsPrRight { get; set; }
        public int EwsPrStright { get; set; }
        public string EwsPriviazkaGeoX { get; set; } = "";
        public string EwsPriviazkaGeoY { get; set; } = "";

        // Запись
        public string RecordCreated { get; set; } = "";
        public int RecordUserCod { get; set; }
        public string RecordStatus { get; set; } = "";
        public string EwsJournal { get; set; } = "";
        public int EwsDisplay { get; set; }
        public int EwsBindDisplay { get; set; }

        // === ВЫЧИСЛЯЕМЫЕ ПОЛЯ ===
        public int MarkerId { get; set; }

        // Отображаемый номер гидранта
        public string DisplayNumber => EwsNumber > 0 ? EwsNumber.ToString() : EwsName;

        // Диаметр трубы (будем заполнять из справочника)
        public string PipeInfo { get; set; } = "";

        // Адрес
        public string AddressText => !string.IsNullOrEmpty(EwsStateNote)
            ? EwsStateNote
            : (!string.IsNullOrEmpty(EwsHouseNumber) ? $"д. {EwsHouseNumber}" : "");

        // Название статуса (из справочника)
        public string StatusName { get; set; } = "Неизвестно";

        // Название организации/владельца (из справочника)
        public string OwnerName { get; set; } = "";

        // Зона
        public int? ZoneId { get; set; }
        public string ZoneName { get; set; } = "Не в зоне";
    }
}