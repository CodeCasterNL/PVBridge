using System;
using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
#pragma warning disable 8618 // Nullable
#pragma warning disable IDE1006 // Naming Styles
namespace CodeCaster.GoodWe.Json
{
    internal class LoginRequest
    {
        public LoginRequest(string account, string password)
        {
            Account = account;
            Password = password;
        }

        public string Account { get; }

        [JsonPropertyName("pwd")]
        public string Password { get; }
    }

    internal class PowerStationMonitorRequest
    {
        public PowerStationMonitorRequest(string powerStationId)
        {
            PowerStationId = powerStationId;
        }

        public string PowerStationId { get; }
    }

    internal class PowerStationListRequest
    {
        public PowerStationListRequest(int pageIndex)
        {
            PageIndex = pageIndex;
        }

        public int PageIndex { get; }
    }

    internal class ReportRequest
    {
        public ReportRequest(string ids, DateTime start, DateTime? end)
        {
            Ids = ids;
            Start = start;
            End = end;
        }

        public string Ids { get; set; }
        public int Range { get; set; }
        public int Type { get; set; }
        [JsonConverter(typeof(SystemTextDateTimeConverter))]
        public DateTime? Start { get; set; }
        [JsonConverter(typeof(SystemTextDateTimeConverter))]
        public DateTime? End { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class ChartDataRequest
    {
        public string qry_time_start { get; set; }
        public string qry_time_end { get; set; }
        public int times { get; set; }
        public int qry_status { get; set; }
        public Pws_Historys[] pws_historys { get; set; }
        public ChartDataRequestTarget[] targets { get; set; }
    }

    public class Pws_Historys
    {
        public string id { get; set; }
        public string pw_name { get; set; }
        public int status { get; set; }
        public string pw_address { get; set; }
        public Pws_Historys_Inverter[] inverters { get; set; }
    }

    public class Pws_Historys_Inverter
    {
        public string sn { get; set; }
        public string name { get; set; }
        public int change_num { get; set; }
        public int change_type { get; set; }
        public object relation_sn { get; set; }
        public object relation_name { get; set; }
        public int status { get; set; }
    }

    public class ChartDataRequestTarget
    {
        public ChartDataRequestTarget() { }

        public ChartDataRequestTarget(string targetKey)
        {
            target_key = targetKey;
        }
        
        public string target_key { get; set; }
    }
}
#pragma warning restore 8618
#pragma warning restore IDE1006 // Naming Styles
