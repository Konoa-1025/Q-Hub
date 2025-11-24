using Newtonsoft.Json;
using Q_Hub.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Q_Hub.Services
{
    internal class NIEDService
    {
        private readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// NIEDから最新のEEWデータを取得して地震情報として解析
        /// </summary>
        public async Task<NIED> FetchAndParseAsync()
        {
            try
            {
                string timeStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                string url = $"https://www.lmoni.bosai.go.jp/monitor/webservice/hypo/eew/{timeStr}.json";

                string json = await _client.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<NIEDRoot>(json);

                return ParseEarthquakeInfo(data);
            }
            catch (Exception ex)
            {
                return new NIED
                {
                    Status = "ERR",
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// NIEDRootから地震情報を解析して構造化
        /// </summary>
        private NIED ParseEarthquakeInfo(NIEDRoot data)
        {
            var info = new NIED();

            // データ取得失敗時
            if (data == null || data.result == null)
            {
                info.Status = "ERR";
                info.Message = "データ取得失敗またはJSON破損";
                return info;
            }

            // result.status に基づいたステータス判定
            if (data.result.status == "success")
            {
                info.Status = "success";
            }
            else if (data.result.status == "none")
            {
                info.Status = "none";
                info.Message = "EEWデータなし";
                return info;
            }
            else
            {
                info.Status = "NG";
                info.Message = data.result.message ?? "不明なエラー";
                return info;
            }

            // success の場合のみ地震情報を解析
            info.Message = data.result.message;

            // 時刻のパース（フォーマット: yyyyMMddHHmmss）
            if (!string.IsNullOrEmpty(data.request_time))
            {
                if (DateTime.TryParseExact(data.request_time, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime reqTime))
                {
                    info.RequestTime = reqTime;
                }
            }

            if (!string.IsNullOrEmpty(data.report_time))
            {
                // report_time は "2022/10/02 00:02:51" 形式
                if (DateTime.TryParseExact(data.report_time, "yyyy/MM/dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime repTime))
                {
                    info.ReportTime = repTime;
                }
            }

            if (!string.IsNullOrEmpty(data.origin_time))
            {
                if (DateTime.TryParseExact(data.origin_time, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orgTime))
                {
                    info.OriginTime = orgTime;
                }
            }

            // 震源情報
            info.RegionName = data.region_name;

            if (double.TryParse(data.latitude, out double lat))
                info.Latitude = lat;
            if (double.TryParse(data.longitude, out double lon))
                info.Longitude = lon;

            info.Depth = data.depth;
            info.Magnitude = data.magunitude; // typo in API: "magunitude"
            info.CalcIntensity = data.calcintensity;

            // フラグ
            info.IsFinal = data.is_final == "1" || data.is_final?.ToLower() == "true";
            info.IsTraining = data.is_training == "1" || data.is_training?.ToLower() == "true";
            info.IsCancel = data.is_cancel == "1" || data.is_cancel?.ToLower() == "true";

            info.ReportNum = data.report_num;
            info.ReportId = data.report_id;

            // 警報フラグ
            

            return info;
        }
    }
}
