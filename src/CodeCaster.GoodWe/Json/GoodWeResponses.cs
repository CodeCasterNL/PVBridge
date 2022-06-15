using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// JSON...
// ReSharper disable UnusedMember.Global ClassNeverInstantiated.Global InconsistentNaming MemberCanBePrivate.Global UnusedAutoPropertyAccessor.Global CollectionNeverUpdated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles

namespace CodeCaster.GoodWe.Json
{
    internal class ResponseBase<TData>
    {
        public bool HasError { get; set; }
        [JsonConverter(typeof(CodeConverter))]
        public string Code { get; set; }
        public string? Msg { get; set; }
        public TData Data { get; set; }
        public Components Components { get; set; }
    }

    internal class Components
    {
        public object Para { get; set; }
        public int LangVer { get; set; }
        public int TimeSpan { get; set; }
        public string? Api { get; set; }
        public string? MsgSocketAdr { get; set; }
    }


    public class LoginResponse
    {
        public string? uid { get; set; }
        public long timestamp { get; set; }
        public string? token { get; set; }
        public string? client { get; set; }
        public string? version { get; set; }
        public string? language { get; set; }
    }

    public class DateFormatSettingsList
    {
        public DateSetting? Selected { get; set; }
    }

    public class DateSetting
    {
        public string id { get; set; }
        public string date_text { get; set; }
    }

    public class PlantData
    {
        public int record { get; set; }
        public List<AddressWithInverters>? list { get; set; }
    }

    public class ReportData
    {
        public string? title { get; set; }
        public Page page { get; set; }
        public Col[] cols { get; set; }
        public Row[] rows { get; set; }
    }


    public class ChartData
    {
        [JsonPropertyName("list")]
        public ChartDataElement[] Data { get; set; }
    }

    public class AddressWithInverters
    {
        public string? id { get; set; }
        public string? pw_name { get; set; }
        public string? pw_address { get; set; }
        public int status { get; set; }
        public List<Inverter>? inverters { get; set; }

        public string Description => $"{pw_name ?? "(nameless plant)"}, {pw_address}";

        public string DisplayString => $"{Description} (status: {status})";
    }

    public class ChartDataElement
    {
        public string? id { get; set; }
        public string? pw_name { get; set; }
        public string? pw_address { get; set; }
        public int status { get; set; }
        public ChartInverter[] inverters { get; set; }
    }

    public class ChartInverter
    {
        public string? sn { get; set; }
        public string? name { get; set; }
        public int change_num { get; set; }
        public int change_type { get; set; }
        public object relation_sn { get; set; }
        public object relation_name { get; set; }
        public int status { get; set; }
        public ChartTarget[] targets { get; set; }
    }

    public class ChartTarget
    {
        public string? target_key { get; set; }
        public string? target_name { get; set; }
        public string? target_unit { get; set; }
        public ChartTargetData[] datas { get; set; }
    }

    public class ChartTargetData
    {
        public string? stat_date { get; set; }
        public string? value { get; set; }
    }

    public class Page
    {
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public int records { get; set; }
        public object sidx { get; set; }
        public object sord { get; set; }
        public int totalPage { get; set; }
    }

    public class Col
    {
        public int index { get; set; }
        public string? label { get; set; }
        public string? rowName { get; set; }
        public int width { get; set; }
        public object formatter { get; set; }
        public object seletele { get; set; }
    }

    public class Row
    {
        public string? plant { get; set; }
        public string? classification { get; set; }
        public DateTime date { get; set; }
        public object date_string { get; set; }
        public float feedinPrice { get; set; }
        public float electricalTariff { get; set; }
        public float capacity { get; set; }
        public float generation { get; set; }
        public float yield { get; set; }
        public float sell { get; set; }
        public float buy { get; set; }
        public float etotalLoad { get; set; }
        public float selfUseRatio { get; set; }
        public float selfUseOfPv { get; set; }
        public float load { get; set; }
        public bool hasLt4 { get; set; }
        public float mockData { get; set; }
        public float radiationDose { get; set; }
    }

    public class PowerStationMonitorData
    {
        public Info info { get; set; }
        public Kpi kpi { get; set; }
        public object[] images { get; set; }
        public Dictionary<string, List<WeatherInfo>> weather { get; set; }
        public List<Inverter> inverter { get; set; }
        public Hjgx hjgx { get; set; }
        public object pre_powerstation_id { get; set; }
        public object nex_powerstation_id { get; set; }
        public Homkit homKit { get; set; }
        public Smuggleinfo smuggleInfo { get; set; }
        public Powerflow powerflow { get; set; }
        public Energestatisticscharts energeStatisticsCharts { get; set; }
        public Soc soc { get; set; }
    }

    public class Info
    {
        public string? powerstation_id { get; set; }
        public string? time { get; set; }
        public string? date_format { get; set; }
        public string? date_format_ym { get; set; }
        public string? stationname { get; set; }
        public string? address { get; set; }
        public string? owner_name { get; set; }
        public string? owner_phone { get; set; }
        public string? owner_email { get; set; }
        public float battery_capacity { get; set; }
        public string? turnon_time { get; set; }
        public string? create_time { get; set; }
        public float capacity { get; set; }
        public float longitude { get; set; }
        public float latitude { get; set; }
        public string? powerstation_type { get; set; }
        public int status { get; set; }
        public bool is_stored { get; set; }
        public bool is_powerflow { get; set; }
        public int charts_type { get; set; }
        public bool has_pv { get; set; }
        public bool has_statistics_charts { get; set; }
        public bool only_bps { get; set; }
        public bool only_bpu { get; set; }
        public float time_span { get; set; }
        public string? pr_value { get; set; }
    }

    public class Kpi
    {
        public float Month_generation { get; set; }
        public float pac { get; set; }
        public float power { get; set; }
        public float total_power { get; set; }
        public float day_income { get; set; }
        public float total_income { get; set; }
        public float yield_rate { get; set; }
        public string? currency { get; set; }
    }

    public class WeatherInfo
    {
        public Daily_Forecast[] daily_forecast { get; set; }
        public Basic basic { get; set; }
        public Update update { get; set; }
        public string? status { get; set; }
    }

    public class Basic
    {
        public string? cid { get; set; }
        public string? location { get; set; }
        public string? cnty { get; set; }
        public string? lat { get; set; }
        public string? lon { get; set; }
        public string? tz { get; set; }
    }

    public class Update
    {
        public string? loc { get; set; }
        public string? utc { get; set; }
    }

    public class Daily_Forecast
    {
        public string? cond_code_d { get; set; }
        public string? cond_code_n { get; set; }
        public string? cond_txt_d { get; set; }
        public string? cond_txt_n { get; set; }
        public string? date { get; set; }
        public string? time { get; set; }
        public string? hum { get; set; }
        public string? pcpn { get; set; }
        public string? pop { get; set; }
        public string? pres { get; set; }
        public string? tmp_max { get; set; }
        public string? tmp_min { get; set; }
        public string? uv_index { get; set; }
        public string? vis { get; set; }
        public string? wind_deg { get; set; }
        public string? wind_dir { get; set; }
        public string? wind_sc { get; set; }
        public string? wind_spd { get; set; }
    }

    public class Hjgx
    {
        public float co2 { get; set; }
        public float tree { get; set; }
        public float coal { get; set; }
    }

    public class Homkit
    {
        public bool homeKitLimit { get; set; }
        public object sn { get; set; }
    }

    public class Smuggleinfo
    {
        public bool isAllSmuggle { get; set; }
        public bool isSmuggle { get; set; }
        public object descriptionText { get; set; }
        public object sns { get; set; }
    }

    public class Powerflow
    {
        public string? pv { get; set; }
        public int pvStatus { get; set; }
        public string? bettery { get; set; }
        public int betteryStatus { get; set; }
        public object betteryStatusStr { get; set; }
        public string? load { get; set; }
        public int loadStatus { get; set; }
        public string? grid { get; set; }
        public int soc { get; set; }
        public string? socText { get; set; }
        public bool hasEquipment { get; set; }
        public int gridStatus { get; set; }
        public bool isHomKit { get; set; }
        public bool isBpuAndInverterNoBattery { get; set; }
    }

    public class Energestatisticscharts
    {
        public float sum { get; set; }
        public float buy { get; set; }
        public float buyPercent { get; set; }
        public float sell { get; set; }
        public float sellPercent { get; set; }
        public float selfUseOfPv { get; set; }
        public float consumptionOfLoad { get; set; }
        public int chartsType { get; set; }
        public bool hasPv { get; set; }
        public bool hasCharge { get; set; }
        public float charge { get; set; }
        public float disCharge { get; set; }
    }

    public class Soc
    {
        public int power { get; set; }
        public int status { get; set; }
    }

    public class Inverter
    {
        [DateTimeFormat("MM/dd/yyyy HH:mm:ss")]
        public DateTime? last_refresh_time { get; set; }

        [DateTimeFormat("MM/dd/yyyy HH:mm:ss")]
        public DateTime? turnon_time { get; set; }

        public InverterData d { get; set; }

        public string? sn { get; set; }
        public Dict dict { get; set; }
        public bool is_stored { get; set; }
        public string? name { get; set; }
        public float in_pac { get; set; }
        public float out_pac { get; set; }
        public float eday { get; set; }
        public float emonth { get; set; }
        public float etotal { get; set; }
        public int status { get; set; }
        public string? releation_id { get; set; }
        public string? type { get; set; }
        public float capacity { get; set; }
        public bool it_change_flag { get; set; }
        public float tempperature { get; set; }
        public string? check_code { get; set; }
        public object next { get; set; }
        public object prev { get; set; }
        public Next_Device next_device { get; set; }
        public Prev_Device prev_device { get; set; }
        public Invert_Full invert_full { get; set; }
        public string? time { get; set; }
        public string? battery { get; set; }
        public float firmware_version { get; set; }
        public string? warning_bms { get; set; }
        public string? soh { get; set; }
        public string? discharge_current_limit_bms { get; set; }
        public string? charge_current_limit_bms { get; set; }
        public string? soc { get; set; }
        public string? pv_input_2 { get; set; }
        public string? pv_input_1 { get; set; }
        public string? back_up_output { get; set; }
        public string? output_voltage { get; set; }
        public string? backup_voltage { get; set; }
        public string? output_current { get; set; }
        public string? output_power { get; set; }
        public string? total_generation { get; set; }
        public string? daily_generation { get; set; }
        public string? battery_charging { get; set; }
        public string? bms_status { get; set; }
        public string? pw_id { get; set; }
        public string? fault_message { get; set; }
        public float battery_power { get; set; }
        public string? point_index { get; set; }
        public Point[] points { get; set; }
        public float backup_pload_s { get; set; }
        public float backup_vload_s { get; set; }
        public float backup_iload_s { get; set; }
        public float backup_pload_t { get; set; }
        public float backup_vload_t { get; set; }
        public float backup_iload_t { get; set; }
        public object etotal_buy { get; set; }
        public object eday_buy { get; set; }
        public object ebattery_charge { get; set; }
        public object echarge_day { get; set; }
        public object ebattery_discharge { get; set; }
        public object edischarge_day { get; set; }
        public float batt_strings { get; set; }
        public object meter_connect_status { get; set; }
        public float mtactivepower_r { get; set; }
        public float mtactivepower_s { get; set; }
        public float mtactivepower_t { get; set; }
        public bool has_tigo { get; set; }
        public bool canStartIV { get; set; }
    }

    public class Dict
    {
        public Left[] left { get; set; }
        public Right[] right { get; set; }
    }

    public class Left
    {
        public bool isHT { get; set; }
        public string? key { get; set; }
        public string? value { get; set; }
        public string? unit { get; set; }
        public int isFaultMsg { get; set; }
        public int faultMsgCode { get; set; }
    }

    public class Right
    {
        public bool isHT { get; set; }
        public string? key { get; set; }
        public string? value { get; set; }
        public string? unit { get; set; }
        public int isFaultMsg { get; set; }
        public int faultMsgCode { get; set; }
    }

    public class InverterData
    {
        public string? pw_id { get; set; }
        public string? capacity { get; set; }
        public string? model { get; set; }
        public string? output_power { get; set; }
        public string? output_current { get; set; }
        public string? grid_voltage { get; set; }
        public string? backup_output { get; set; }
        public string? soc { get; set; }
        public string? soh { get; set; }
        public string? last_refresh_time { get; set; }
        public string? work_mode { get; set; }
        public string? dc_input1 { get; set; }
        public string? dc_input2 { get; set; }
        public string? battery { get; set; }
        public string? bms_status { get; set; }
        public string? warning { get; set; }
        public string? charge_current_limit { get; set; }
        public string? discharge_current_limit { get; set; }
        public float firmware_version { get; set; }
        public string? creationDate { get; set; }
        public float eDay { get; set; }
        public float eTotal { get; set; }
        public float pac { get; set; }
        public float hTotal { get; set; }
        public float vpv1 { get; set; }
        public float vpv2 { get; set; }
        public float vpv3 { get; set; }
        public float vpv4 { get; set; }
        public float ipv1 { get; set; }
        public float ipv2 { get; set; }
        public float ipv3 { get; set; }
        public float ipv4 { get; set; }
        public float vac1 { get; set; }
        public float vac2 { get; set; }
        public float vac3 { get; set; }
        public float iac1 { get; set; }
        public float iac2 { get; set; }
        public float iac3 { get; set; }
        public float fac1 { get; set; }
        public float fac2 { get; set; }
        public float fac3 { get; set; }
        public float istr1 { get; set; }
        public float istr2 { get; set; }
        public float istr3 { get; set; }
        public float istr4 { get; set; }
        public float istr5 { get; set; }
        public float istr6 { get; set; }
        public float istr7 { get; set; }
        public float istr8 { get; set; }
        public float istr9 { get; set; }
        public float istr10 { get; set; }
        public float istr11 { get; set; }
        public float istr12 { get; set; }
        public float istr13 { get; set; }
        public float istr14 { get; set; }
        public float istr15 { get; set; }
        public float istr16 { get; set; }
    }

    public class Next_Device
    {
        public object sn { get; set; }
        public bool isStored { get; set; }
    }

    public class Prev_Device
    {
        public object sn { get; set; }
        public bool isStored { get; set; }
    }

    public class Invert_Full
    {
        public string? sn { get; set; }
        public string? powerstation_id { get; set; }
        public string? name { get; set; }
        public string? model_type { get; set; }
        public int change_type { get; set; }
        public int change_time { get; set; }
        public float capacity { get; set; }
        public float eday { get; set; }
        public float iday { get; set; }
        public float etotal { get; set; }
        public float itotal { get; set; }
        public float hour_total { get; set; }
        public int status { get; set; }
        public long turnon_time { get; set; }
        public float pac { get; set; }
        public float tempperature { get; set; }
        public float vpv1 { get; set; }
        public float vpv2 { get; set; }
        public float vpv3 { get; set; }
        public float vpv4 { get; set; }
        public float ipv1 { get; set; }
        public float ipv2 { get; set; }
        public float ipv3 { get; set; }
        public float ipv4 { get; set; }
        public float vac1 { get; set; }
        public float vac2 { get; set; }
        public float vac3 { get; set; }
        public float iac1 { get; set; }
        public float iac2 { get; set; }
        public float iac3 { get; set; }
        public float fac1 { get; set; }
        public float fac2 { get; set; }
        public float fac3 { get; set; }
        public float istr1 { get; set; }
        public float istr2 { get; set; }
        public float istr3 { get; set; }
        public float istr4 { get; set; }
        public float istr5 { get; set; }
        public float istr6 { get; set; }
        public float istr7 { get; set; }
        public float istr8 { get; set; }
        public float istr9 { get; set; }
        public float istr10 { get; set; }
        public float istr11 { get; set; }
        public float istr12 { get; set; }
        public float istr13 { get; set; }
        public float istr14 { get; set; }
        public float istr15 { get; set; }
        public float istr16 { get; set; }
        public long last_time { get; set; }
        public float vbattery1 { get; set; }
        public float ibattery1 { get; set; }
        public float pmeter { get; set; }
        public float soc { get; set; }
        public float soh { get; set; }
        public object bms_discharge_i_max { get; set; }
        public float bms_charge_i_max { get; set; }
        public int bms_warning { get; set; }
        public int bms_alarm { get; set; }
        public int battary_work_mode { get; set; }
        public int workmode { get; set; }
        public float vload { get; set; }
        public float iload { get; set; }
        public float firmwareversion { get; set; }
        public float pbackup { get; set; }
        public float seller { get; set; }
        public float buy { get; set; }
        public object yesterdaybuytotal { get; set; }
        public object yesterdaysellertotal { get; set; }
        public object yesterdayct2sellertotal { get; set; }
        public object yesterdayetotal { get; set; }
        public object yesterdayetotalload { get; set; }
        public float thismonthetotle { get; set; }
        public float lastmonthetotle { get; set; }
        public float ram { get; set; }
        public float outputpower { get; set; }
        public int fault_messge { get; set; }
        public bool isbuettey { get; set; }
        public bool isbuetteybps { get; set; }
        public bool isbuetteybpu { get; set; }
        public bool isESUOREMU { get; set; }
        public float backUpPload_S { get; set; }
        public float backUpVload_S { get; set; }
        public float backUpIload_S { get; set; }
        public float backUpPload_T { get; set; }
        public float backUpVload_T { get; set; }
        public float backUpIload_T { get; set; }
        public object eTotalBuy { get; set; }
        public object eDayBuy { get; set; }
        public object eBatteryCharge { get; set; }
        public object eChargeDay { get; set; }
        public object eBatteryDischarge { get; set; }
        public object eDischargeDay { get; set; }
        public float battStrings { get; set; }
        public object meterConnectStatus { get; set; }
        public float mtActivepowerR { get; set; }
        public float mtActivepowerS { get; set; }
        public float mtActivepowerT { get; set; }
        public string? dataloggersn { get; set; }
        public object equipment_name { get; set; }
        public bool hasmeter { get; set; }
        public object meter_type { get; set; }
        public object? pre_hour_lasttotal { get; set; }
        public object? pre_hour_time { get; set; }
        public float? current_hour_pv { get; set; }
    }

    public class Point
    {
        public int target_index { get; set; }
        public string? target_name { get; set; }
        public string? display { get; set; }
        public string? unit { get; set; }
        public string? target_key { get; set; }
        public string? text_cn { get; set; }
        public object target_sn_six { get; set; }
        public object target_sn_seven { get; set; }
        public object target_type { get; set; }
        public object storage_name { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
// ReSharper enable UnusedMember.Global
