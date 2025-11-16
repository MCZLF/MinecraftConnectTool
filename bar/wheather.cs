using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class wheatherbar : UserControl
    {
        public wheatherbar()
        {
            InitializeComponent();
#pragma warning disable CS4014
            GetWeatherAsync();
#pragma warning restore CS4014
        }
        private static Font LabelFont { get; } = new Font("Microsoft YaHei UI", 9F);
        private const string WEATHER_API_URL = "https://wttr.in/?format=%l:+%t+%w+%h+%P+%C&lang=zh"; // WTTR.in API URL
        private const string MOOD_API_URL = "https://uapis.cn/api/say"; // Mood API URL

        private void label1_MouseDoubleClick(object sender, EventArgs e)
        {
            AntdUI.Message.info(Program.MainForm, "正在刷新天气~", autoClose: 5, font:LabelFont);
            label1.Text = $"正在刷新中~";
#pragma warning disable CS4014
            GetWeatherAsync();
#pragma warning restore CS4014
        }

        private async Task GetWeatherAsync()
        {
            try
            {
                // 获取天气信息
                string weatherApiResponse = await GetApiDataAsync(WEATHER_API_URL);
                string moodApiResponse = await GetApiDataAsync(MOOD_API_URL);

                // 解析天气信息
                string[] weatherData = weatherApiResponse.Split(':');
                string location = weatherData[0].Trim();
                string details = weatherData[1].Trim();

                string[] detailsData = details.Split(' ');
                string temperature = detailsData[0].Replace("+", "") + ""; // 添加摄氏度标志
                string wind = detailsData[1];
                string humidity = detailsData[2];
                string pressure = detailsData[3] + "hPa"; // 添加压强单位
                string weatherCondition = detailsData[4]; // 天气情况
                string warmWords = moodApiResponse.Trim();
                label1.Text = $"当前位置：{location} | 天气：{weatherCondition} | 温度：{temperature} | 湿度：{humidity} | 风向：{wind} | 压强：{pressure} | \n心情寄语：{warmWords}";
            }
            catch (Exception ex)
            {
                label1.Text = $"获取天气信息失败：{ex.Message}";
            }
        }

        private async Task<string> GetApiDataAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private void alertbar_Load(object sender, EventArgs e)
        {

        }
    }
}