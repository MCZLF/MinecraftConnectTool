using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class PanelAlert : UserControl
    {
        public PanelAlert()
        {
            InitializeComponent();
        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void PanelAlert_Load(object sender, EventArgs e)
        {
            label1.Text = "正在加载公告...";
            LoadAnnouncementAsync();
        }

        private async void LoadAnnouncementAsync()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var json = await client.GetStringAsync(
                        "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/PanelAlert"
                    );

                    var config = JObject.Parse(json);

                    // 获取TagID并直接设置到label2（单独处理线程安全）
                    string tagId = config["TagID"]?.ToString() ?? "未获取";
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => label2.Text = "重要云公告|TagID:" + tagId));
                    }
                    else
                    {
                        label2.Text = "重要云公告|TagID:" + tagId;
                    }

                    string text = config["Text"]?.ToString() ?? "暂无公告内容";

                    // 处理换行符
                    text = text.Replace("\\n", Environment.NewLine)
                               .Replace("\n", Environment.NewLine);

                    // 更新Label1（原有方法不变）
                    UpdateLabel(text);
                }
            }
            catch (Exception ex)
            {
                // 异常时Label2显示失败状态
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => label2.Text = "重要云公告|TagID:获取失败"));
                }
                else
                {
                    label2.Text = "重要云公告|TagID:获取失败";
                }

                // 抓取异常详情显示到Label1
                string errorText = $"公告获取失败\n【{ex.GetType().Name}】\n{ex.Message}";
                UpdateLabel(errorText);
            }
        }
        // 抽离UI更新方法，避免重复代码
        private void UpdateLabel(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => label1.Text = text));
            }
            else
            {
                label1.Text = text;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
