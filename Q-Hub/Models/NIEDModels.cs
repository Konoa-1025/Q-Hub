using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Hub.Models
{
    public class NIEDRoot
    {
        public string request_time { get; set; }
        public string request_hypo_type { get; set; }
        public string report_num { get; set; }
        public string report_id { get; set; }
        public string report_time { get; set; }       // 追加
        public string origin_time { get; set; }
        public string longitude { get; set; }
        public string latitude { get; set; }
        public string depth { get; set; }
        public string magunitude { get; set; }        // API の typo そのまま
        public string region_code { get; set; }
        public string region_name { get; set; }
        public string calcintensity { get; set; }
        public string is_final { get; set; }
        public string is_training { get; set; }
        public string is_cancel { get; set; }
        public string alertflg { get; set; }          // 追加（警報フラグ）
        public Result result { get; set; }
        public Security security { get; set; }
    }

    public class Result
    {
        public string status { get; set; }
        public string message { get; set; }
        public bool is_auth { get; set; }
    }

    public class Security
    {
        public string hash { get; set; }
        public string realm { get; set; }
    }
}
