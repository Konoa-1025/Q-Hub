using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Hub.Models
{
    public class NIED
    {
        public string Status { get; set; }          // "success", "none", "ERR", "NG"
        public string Message { get; set; }         // エラーメッセージ等
        public DateTime? RequestTime { get; set; }
        public DateTime? OriginTime { get; set; }
        public string RegionName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Depth { get; set; }
        public string Magnitude { get; set; }
        public string CalcIntensity { get; set; }
        public bool IsFinal { get; set; }
        public bool IsTraining { get; set; }
        public bool IsCancel { get; set; }
        public string ReportNum { get; set; }
        public string ReportId { get; set; }
        public DateTime? ReportTime { get; set; }
        public string AlertFlag { get; set; }       // ← 追加

        // 簡易表示用
        public string ToShortString()
        {
            if (Status != "success")
                return $"Status: {Status} - {Message}";

            return $"{RegionName} M{Magnitude} 深さ{Depth} 震度{CalcIntensity} {(IsFinal ? "最終" : "続報")}";
        }
    }
}
