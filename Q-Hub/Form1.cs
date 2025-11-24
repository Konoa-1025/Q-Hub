using Newtonsoft.Json;
using Q_Hub.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Q_Hub
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeLoadingOverlay();
        }
        const string version = "1.0.0";
        int pages = 1;
        PerformanceCounter download;
        PerformanceCounter upload;
        string nicName;
        string time;


        private void InitializeLoadingOverlay()
        {
            // 現在時刻をフィールドに設定して簡易ログ
            time = DateTime.Now.ToString("【HH:mm:ss.fff】");
            if (logbox != null)
            {
                logbox.AppendText($"{time} 初期化中\r\n");
                logbox.AppendText($"{time} E-Hubの起動・・・\r\n");
                logbox.AppendText($"{time} ■■アップデートの確認■■\r\n");
                ehubtic.Start();
                //if(version == )
                logbox.AppendText($"{time} ■■バージョン：1.0.0■■\r\n");
            }
        }

        private void logtime_Tick(object sender, EventArgs e)
        {
            time = DateTime.Now.ToString("【HH:mm:ss.fff】");
        }

        void pagechage()
        {
            Panel[] panellist = { P1, P2, P3, P4, P5, P6 };
            pages = pages - 1;
            if (pages < 0 || pages >= mainpage.TabPages.Count) pages = 0;
            mainpage.SelectedTab = mainpage.TabPages[pages];
            Debug.WriteLine($"メインページを{pages}に変更");

            foreach (Panel p in panellist)
            {
                p.BackColor = Color.Silver;
            }
            panellist[pages].BackColor = Color.SkyBlue;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // 時刻未設定ならセット
            if (string.IsNullOrEmpty(time))
                time = DateTime.Now.ToString("【HH:mm:ss.fff】");

            if (logbox != null)
                logbox.AppendText($"{time} ネットワークインターフェース検出...\r\n");

            // NIC 名の取得は重い可能性があるためバックグラウンドで一度だけ実行
            string foundNic = await Task.Run(() =>
            {
                try
                {
                    var category = new PerformanceCounterCategory("Network Interface");
                    var nics = category.GetInstanceNames();
                    return (nics != null && nics.Length > 0) ? nics[0] : null;
                }
                catch
                {
                    return null;
                }
            });

            if (string.IsNullOrEmpty(foundNic))
            {
                if (logbox != null)
                    logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} ネットワークインターフェースが見つかりません\r\n");

                if (DlLb != null) DlLb.Text = "N/A";
                if (UpLb != null) UpLb.Text = "N/A";
            }
            else
            {
                nicName = foundNic;
                if (logbox != null)
                    logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIC: {nicName}\r\n");

                try
                {
                    download = new PerformanceCounter("Network Interface", "Bytes Received/sec", nicName);
                    upload = new PerformanceCounter("Network Interface", "Bytes Sent/sec", nicName);
                }
                catch (Exception ex)
                {
                    if (logbox != null)
                        logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} パフォーマンスカウンタ初期化失敗: {ex.Message}\r\n");
                    download = null;
                    upload = null;
                }
            }

            try
            {
                chartNet?.Series.Clear();
                SetupChart();
            }
            catch (Exception ex)
            {
                if (logbox != null)
                    logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} グラフ初期化失敗: {ex.Message}\r\n");
            }

            if (download != null && upload != null)
            {
                Network.Start();
            }
            else
            {
                if (logbox != null)
                    logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} ネットワーク監視は開始されません\r\n");
            }

            loadAPI();
        }

        private async void loadAPI()
        {
            await Task.Delay(200);
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED（防災科学技術研究所）API取得開始\r\n");
            NIEDtic.Start();
            await Task.Delay(100);
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} Kwatch API取得開始\r\n");
            Kwatchtic.Start();
            await Task.Delay(100);
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} P2PQuakeAPI取得開始\r\n");
            P2PQauketic.Start();
            await Task.Delay(100);
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} WolfxAPI取得開始\r\n");
            Wolfxtic.Start();
        }

        private void SetupChart()
        {
            chartNet.Series.Clear();

            var sDown = chartNet.Series.Add("DOWN");
            sDown.ChartType = SeriesChartType.FastLine;
            sDown.BorderWidth = 2;
            sDown.Color = Color.DeepSkyBlue;

            var sUp = chartNet.Series.Add("UP");
            sUp.ChartType = SeriesChartType.FastLine;
            sUp.BorderWidth = 2;
            sUp.Color = Color.OrangeRed;

            var area = chartNet.ChartAreas[0];

            // X軸（時間）
            area.AxisX.Enabled = AxisEnabled.True;
            area.AxisX.MajorGrid.LineColor = Color.Gray;
            area.AxisX.Minimum = 0;
            area.AxisX.Maximum = 60;

            // Y軸（通信量）
            area.AxisY.Title = "";
            area.AxisY.MajorGrid.LineColor = Color.LightGray;

            // 凡例を消す
            if (chartNet.Legends.Count > 0) chartNet.Legends[0].Enabled = false;

            // フォント小さく
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 6);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 6);
            area.AxisY.TitleFont = new Font("Segoe UI", 7);

            area.Position.Auto = false;
            area.Position = new ElementPosition(0, 0, 100, 100);

            area.InnerPlotPosition.Auto = false;
            area.InnerPlotPosition = new ElementPosition(2, 2, 96, 96);

            // 軸の線・文字・目盛りを全部OFF
            area.AxisX.LineWidth = 0;
            area.AxisY.LineWidth = 0;
            area.AxisX.LabelStyle.Enabled = false;
            area.AxisY.LabelStyle.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisY.MajorTickMark.Enabled = false;
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            SCPL.Location = new System.Drawing.Point(SCPL.Location.X, -vScrollBar1.Value);
        }

        private void label1_Click(object sender, EventArgs e)
        {
            pages = 1;
            pagechage();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            pages = 1;
            pagechage();
        }

        private void P1_Click(object sender, EventArgs e)
        {
            pages = 1;
            pagechage();
        }

        private void label4_Click(object sender, EventArgs e)
        {
            pages = 2;
            pagechage();
        }

        private void label3_Click(object sender, EventArgs e)
        {
            pages = 2;
            pagechage();
        }

        private void P2_Click(object sender, EventArgs e)
        {
            pages = 2;
            pagechage();
        }

        private void label6_Click(object sender, EventArgs e)
        {
            pages = 3;
            pagechage();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            pages = 3;
            pagechage();
        }

        private void P3_Click(object sender, EventArgs e)
        {
            pages = 3;
            pagechage();
        }

        private void label8_Click(object sender, EventArgs e)
        {
            pages = 4;
            pagechage();
        }

        private void label7_Click(object sender, EventArgs e)
        {
            pages = 4;
            pagechage();
        }

        private void P4_Click(object sender, EventArgs e)
        {
            pages = 4;
            pagechage();
        }

        private void label10_Click(object sender, EventArgs e)
        {
            pages = 5;
            pagechage();
        }

        private void label9_Click(object sender, EventArgs e)
        {
            pages = 5;
            pagechage();
        }

        private void P5_Click(object sender, EventArgs e)
        {
            pages = 5;
            pagechage();
        }

        private void label12_Click(object sender, EventArgs e)
        {
            pages = 6;
            pagechage();
        }

        private void label11_Click(object sender, EventArgs e)
        {
            pages = 6;
            pagechage();
        }

        private void P6_Click(object sender, EventArgs e)
        {
            pages = 6;
            pagechage();
        }

        private void Network_Tick(object sender, EventArgs e)
        {
            if (download == null || upload == null)
                return;

            float down = download.NextValue();
            float up = upload.NextValue();
            float downMbps = (down * 8) / 1_000_000f;
            float upMbps = (up * 8) / 1_000_000f;

            Debug.WriteLine($"Download: {downMbps} Mbps, Upload: {upMbps} Mbps");

            // ログボックスの肥大化を防ぐ
            if (logHBbox != null)
            {
                try
                {
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} ダウンロード: {downMbps.ToString("G2")} Mbps, アップロード: {upMbps.ToString("G2")} Mbps\r\n");
                    if (logHBbox.TextLength > 20000)
                        logHBbox.Clear();
                }
                catch { }
            }

            if (DlLb != null) DlLb.Text = downMbps.ToString("G2") + " Mbps";
            if (UpLb != null) UpLb.Text = upMbps.ToString("G2") + " Mbps";

            int maxPoints = 60;

            if (chartNet.Series.IndexOf("DOWN") >= 0 && chartNet.Series.IndexOf("UP") >= 0)
            {
                chartNet.Series["DOWN"].Points.AddY(downMbps);
                chartNet.Series["UP"].Points.AddY(upMbps);

                if (chartNet.Series["DOWN"].Points.Count > maxPoints)
                {
                    chartNet.Series["DOWN"].Points.RemoveAt(0);
                    chartNet.Series["UP"].Points.RemoveAt(0);
                }

                chartNet.ChartAreas[0].AxisX.Minimum = 0;
                chartNet.ChartAreas[0].AxisX.Maximum = maxPoints;

                double maxValue = Math.Max(
                    chartNet.Series["DOWN"].Points.Max(p => p.YValues[0]),
                    chartNet.Series["UP"].Points.Max(p => p.YValues[0])
                );

                if (maxValue < 1) maxValue = 1;

                chartNet.ChartAreas[0].AxisY.Minimum = 0;
                chartNet.ChartAreas[0].AxisY.Maximum = Math.Ceiling(maxValue * 1.2);
            }
        }

        private void label16_Click(object sender, EventArgs e)
        {
            if (logHBbox != null) logHBbox.Visible = false;
            label16.BackColor = Color.SkyBlue;
            label17.BackColor = Color.Silver;
        }

        private void label17_Click(object sender, EventArgs e)
        {
            if (logHBbox != null) logHBbox.Visible = true;
            label17.BackColor = Color.SkyBlue;
            label16.BackColor = Color.Silver;
        }

        private readonly NIEDService _niedService = new NIEDService();
        private async void NIEDtic_Tick(object sender, EventArgs e)
        {
            
            NItimeB.Text = DateTime.Now.ToString("yyyyMMddHHmmss");
            NINet.Visible = true;
            await Task.Delay(100);
            NINet.Visible = false;
            try
            {
                var NIEDInfo = await _niedService.FetchAndParseAsync();

                logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED APIステータス取得：{NIEDInfo.Status}\r\n");

                if (NIEDInfo.Status == "success")
                {
                    NIEDLb.Text = "OK";
                    NIEDLb.ForeColor = Color.Green;

                    if (NIEDInfo.IsCancel == false)
                    {
                        if (NIEDInfo.AlertFlag == "予報")
                        {
                            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 【予報】{NIEDInfo.RegionName}で地震、推定震度{NIEDInfo.CalcIntensity}　\r\n");
                        }
                        else if (NIEDInfo.AlertFlag == "警報")
                        {
                            // 緊急警報（最大警戒）
                            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 【警報】{NIEDInfo.RegionName}で地震、推定震度{NIEDInfo.CalcIntensity}　強い揺れに警戒してください！\r\n");
                        }
                        else
                        {
                            //logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 発表されている情報はありませんでした。\r\n");
                        }

                    }
                    else
                    {
                        logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 【キャンセル】地震情報はキャンセルされました。\r\n");
                    }





                    // 画面反映
                    NIGi.Text = NIEDInfo.ReportTime?.ToString("yyyy/MM/dd HH:mm:ss");
                    NIOr.Text = NIEDInfo.OriginTime?.ToString("yyyy/MM/dd HH:mm:ss");
                    NISt.Text = NIEDInfo.Status;
                    NIAl.Text = NIEDInfo.AlertFlag;
                    NITr.Text = NIEDInfo.IsTraining.ToString();
                    NICa.Text = NIEDInfo.IsCancel.ToString();
                    NIFi.Text = NIEDInfo.IsFinal.ToString();
                    NINu.Text = NIEDInfo.ReportNum;
                    NIRe.Text = NIEDInfo.RegionName;
                    NISi.Text = NIEDInfo.CalcIntensity;
                    NIMg.Text = NIEDInfo.Magnitude;
                    NIDe.Text = NIEDInfo.Depth;
                    NIK.Text = NIEDInfo.Latitude.ToString();
                    NII.Text = NIEDInfo.Longitude.ToString();
                }
                else if (NIEDInfo.Status == "none")
                {
                    NIEDLb.Text = "WARN";
                    NIEDLb.ForeColor = Color.Yellow;
                }
                else if (NIEDInfo.Status == "NG")
                {
                    NIEDLb.Text = "NG";
                    NIEDLb.ForeColor = Color.Red;
                    logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED NG: {NIEDInfo.Message}\r\n");
                }
                else // "ERR"
                {
                    NIEDLb.Text = "ERR";
                    NIEDLb.ForeColor = Color.Red;
                    EER(NIEDInfo.Message, "NIED");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NIED API取得エラー: {ex.Message}");
                NIEDLb.Text = "ERR";
                EER(ex.Message, "NIED");

                if (NIEDLb.ForeColor == Color.Red)
                {
                    NIEDLb.ForeColor = Color.Black;
                }
                else
                {
                    NIEDLb.ForeColor = Color.Red;
                }
            }
        }

        void EER(string msg, string type)
        {
            EERLb.Text += $"{DateTime.Now:【HH:mm:ss.fff】}" + type + "：" + msg;
            EERLb.Visible = true;
            EERPl.BackColor = Color.Brown;
            EERPl.ForeColor = Color.White;
        }

        private void NIEDLb_TextChanged(object sender, EventArgs e)
        {
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED（防災科学技術研究所）API：{NIEDLb.Text}\r\n");
        }

        private async void P2PQuake_Tick(object sender, EventArgs e)
        {

            P2PNet.Visible = true;
            await Task.Delay(100);
            P2PNet.Visible = false;
            try
            {
                var client = new HttpClient();
                string url = $"https://api.p2pquake.net/v2/history?codes=551&codes=552&codes=554&codes=556&codes=562&limit=1";
                string json = await client.GetStringAsync(url);

                List<P2PRoot> list = JsonConvert.DeserializeObject<List<P2PRoot>>(json);
                var latest = list?.FirstOrDefault();

                if (latest != null)
                {
                    P2PLb.Text = "OK";
                    P2PLb.ForeColor = Color.Green;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} P2PQuake    APIステータス取得：OK\r\n");
                }
                else
                {
                    P2PLb.Text = "NG";
                    P2PLb.ForeColor = Color.Red;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} P2PQuake    APIステータス取得：NG\r\n");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"P2PQuake API取得エラー: {ex.Message}");
                P2PLb.Text = "ERR";
                EER(ex.Message, "P2PQuake");
            }
        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void EERLb_Click(object sender, EventArgs e)
        {

        }

        private void P2PLb_TextChanged(object sender, EventArgs e)
        {
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} P2PQuake API：{P2PLb.Text}\r\n");
        }

        private void EERLb_Click_1(object sender, EventArgs e)
        {
            if (EERLb.Text != "")
            {
                Clipboard.SetText(EERLb.Text);
                EERLb.Text = "コピーしました！";
            }
        }

        private void KLb_TextChanged(object sender, EventArgs e)
        {
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} Kwatch API：{KLb.Text}\r\n");
        }

        private async void Kwatchtic_Tick(object sender, EventArgs e)
        {
            //Kwatch処理

            KwatchNet.Visible = true;
            await Task.Delay(100);
            KwatchNet.Visible = false;
            try
            {
                var client = new HttpClient();
                string url = $"https://kwatch-24h.net/EQLevel.json";
                string json = await client.GetStringAsync(url);

                KwatchRoot latest = JsonConvert.DeserializeObject<KwatchRoot>(json);

                if (latest != null)
                {
                    KLb.Text = "OK";
                    KLb.ForeColor = Color.Green;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} Kwatch    APIステータス取得：OK\r\n");
                }
                else
                {
                    KLb.Text = "NG";
                    KLb.ForeColor = Color.Red;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} Kwatch    APIステータス取得：NG\r\n");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Kwatch API取得エラー: {ex.Message}");
                KLb.Text = "ERR";
                EER(ex.Message, "Kwatch");
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void EERLb_TextChanged(object sender, EventArgs e)
        {

        }

        private async void Wolfxtic_Tick(object sender, EventArgs e)
        {
            WolfxNet.Visible = true;
            await Task.Delay(100);
            WolfxNet.Visible = false;
            try
            {
                var client = new HttpClient();
                string url = $"https://api.wolfx.jp/jma_eew.json";
                string json = await client.GetStringAsync(url);

                WolfxRoot latest = JsonConvert.DeserializeObject<WolfxRoot>(json);

                if (latest != null)
                {
                    WolfxLb.Text = "OK";
                    WolfxLb.ForeColor = Color.Green;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} WolfxAPIステータス取得：OK\r\n");
                }
                else
                {
                    WolfxLb.Text = "NG";
                    WolfxLb.ForeColor = Color.Red;
                    logHBbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} WolfxAPIステータス取得：NG\r\n");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Wolfx API取得エラー: {ex.Message}");
                WolfxLb.Text = "ERR";
                EER(ex.Message, "Wolfx");
            }
        }

        private void WolfxLb_TextChanged(object sender, EventArgs e)
        {
            logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} WolfxAPI API：{WolfxLb.Text}\r\n");
        }

        private async void ehubtic_Tick(object sender, EventArgs e)
        {
            var client = new HttpClient();
            string json = await client.GetStringAsync("https://raw.githubusercontent.com/Konoa-1025/Q-Hub/refs/heads/master/appdete.json");
            var info = JsonConvert.DeserializeObject<AppUpdateInfo>(json);

            //if (info.latest_version != currentVersion)
            //{
            //    // アップデートあり
            //}

        }

        private void label28_Click(object sender, EventArgs e)
        {
            NIPage.SelectedTab = NIPage.TabPages[0];
            NIchar.BackColor = Color.SkyBlue;
            NIpic.BackColor = Color.Silver;
            Kwlb.BackColor = Color.Silver;
        }

        private void label27_Click(object sender, EventArgs e)
        {
            NIPage.SelectedTab = NIPage.TabPages[1];
            NIchar.BackColor = Color.Silver;
            NIpic.BackColor = Color.SkyBlue;
            Kwlb.BackColor = Color.Silver;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void NINet_VisibleChanged(object sender, EventArgs e)
        {

        }

        private void label38_Click(object sender, EventArgs e)
        {
            NIPage.SelectedTab = NIPage.TabPages[2];
            NIchar.BackColor = Color.Silver;
            NIpic.BackColor = Color.Silver;
            Kwlb.BackColor = Color.SkyBlue;
        }

        private void NINu_TextChanged(object sender, EventArgs e)
        {

        }

        private void NINu_TextAlignChanged(object sender, EventArgs e)
        {
            
        }

        private void NINu_TextChanged_1(object sender, EventArgs e)
        {
            int NINumber;
            int OldNINNumber = 0;   // ← 初期値を入れる！

            if (int.TryParse(NINu.Text, out NINumber))
            {
                // OK
            }
            else
            {
                // NG
            }

            if (NINumber > OldNINNumber)
            {
                logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 報数逆戻りが発生しました。\r\n");
            }

            // 次回比較のため更新
            OldNINNumber = NINumber;

            if (NIAl.Text == "予報")
            {

            }
            else if (NIAl.Text == "警報")
            {

            }
            else
            {
                logbox.AppendText($"{DateTime.Now:【HH:mm:ss.fff】} NIED 緊急地震速報の発表はありません。\r\n");
            }
        }

        //NIED
        public class NIEDRoot
        {
            public string request_time { get; set; }
            public string request_hypo_type { get; set; }
            public string report_num { get; set; }
            public string report_id { get; set; }
            public string report_time { get; set; }
            public string origin_time { get; set; }
            public string longitude { get; set; }
            public string latitude { get; set; }
            public string depth { get; set; }
            public string magunitude { get; set; }
            public string region_code { get; set; }
            public string region_name { get; set; }
            public string calcintensity { get; set; }
            public string is_final { get; set; }
            public string is_training { get; set; }
            public string is_cancel { get; set; }
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

        //P2PQuake
        public class P2PRoot
        {
            public int code { get; set; }
            public Comments comments { get; set; }
            public Earthquake earthquake { get; set; }
            public string id { get; set; }
            public Issue issue { get; set; }
            public List<Point> points { get; set; }
            public string time { get; set; }
            public Timestamp timestamp { get; set; }
            public string user_agent { get; set; }
            public string ver { get; set; }
        }

        public class Comments
        {
            public string freeFormComment { get; set; }
        }

        public class Earthquake
        {
            public string domesticTsunami { get; set; }
            public string foreignTsunami { get; set; }
            public Hypocenter hypocenter { get; set; }
            public int maxScale { get; set; }
            public string time { get; set; }
        }

        public class Hypocenter
        {
            public int depth { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public double magnitude { get; set; }
            public string name { get; set; }
        }

        public class Issue
        {
            public string correct { get; set; }
            public string source { get; set; }
            public string time { get; set; }
            public string type { get; set; }
        }

        public class Point
        {
            public string addr { get; set; }
            public bool isArea { get; set; }
            public string pref { get; set; }
            public int scale { get; set; }
        }

        public class Timestamp
        {
            public string convert { get; set; }
            public string register { get; set; }
        }

        //Kwatch
        public class KwatchRoot
        {
            public int l { get; set; }
            public int g { get; set; }
            public int y { get; set; }
            public int r { get; set; }
            public string t { get; set; }
            public int e { get; set; }
        }

        //Wolfx
        public class WolfxRoot
        {
            public string Title { get; set; }
            public string CodeType { get; set; }
            public WolfxIssue Issue { get; set; }
            public string EventID { get; set; }
            public int Serial { get; set; }
            public string AnnouncedTime { get; set; }
            public string OriginTime { get; set; }
            public string Hypocenter { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Magunitude { get; set; }
            public int Depth { get; set; }
            public string MaxIntensity { get; set; }
            public Accuracy Accuracy { get; set; }
            public MaxIntChange MaxIntChange { get; set; }
            public List<object> WarnArea { get; set; }
            public bool isSea { get; set; }
            public bool isTraining { get; set; }
            public bool isAssumption { get; set; }
            public bool isWarn { get; set; }
            public bool isFinal { get; set; }
            public bool isCancel { get; set; }
            public string OriginalText { get; set; }
            public string Pond { get; set; }
        }

        public class WolfxIssue
        {
            public string Source { get; set; }
            public string Status { get; set; }
        }

        public class Accuracy
        {
            public string Epicenter { get; set; }
            public string Depth { get; set; }
            public string Magnitude { get; set; }
        }

        public class MaxIntChange
        {
            public string String { get; set; }
            public string Reason { get; set; }
        }

        //ehub
        public class AppUpdateInfo
        {
            public string app_name { get; set; }
            public string latest_version { get; set; }
            public bool force_update { get; set; }
            public string download_url { get; set; }
            public string release_date { get; set; }
            public List<string> changelog { get; set; }
            public string notice { get; set; }
        }

        
    }
}

