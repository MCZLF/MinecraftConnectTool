using System.Windows.Forms;
using AntdUI;
//用usercontrol实在不方便干脆用一个类强行写算了
namespace MinecraftConnectTool
{
    internal class InteractiveModal
    {
        public string ShowModal(Form parentForm, string promptText)
        {
            // 创建 AntdUI 的 Panel 作为 Modal 的内容容器
            var contentPanel = new AntdUI.Panel
            {
                Width = 280,
                Height = 73, // 高度是临时Panel的，太大了有白边
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                BackColor = System.Drawing.Color.White,
                Padding = new System.Windows.Forms.Padding(10)
            };
            var promptLabel = new AntdUI.Label
            {
                Text = promptText,
                Font = contentPanel.Font,
                AutoSize = true, // 自动调整大小以适应文本
                Location = new System.Drawing.Point(10, 10)
            };
            var inputBox = new Input
            {
                Width = 260,
                Height = 32,
                Font = contentPanel.Font,
                PlaceholderText = "请输入内容",
                PlaceholderColor = System.Drawing.Color.FromArgb(153, 153, 153, 153),
                Location = new System.Drawing.Point(10, 34) // 参数我是按1600 900去调的，不知道其他分辨率有什么问题
            };
            contentPanel.Controls.Add(promptLabel);
            contentPanel.Controls.Add(inputBox);

            // 配置 Modal
            var modalConfig = Modal.config(parentForm, "请输入文本", contentPanel);
            modalConfig.CloseIcon = true;
            modalConfig.MaskClosable = false;
            modalConfig.Draggable = false;
            modalConfig.Font = Program.AlertFont;
            modalConfig.Width = 300;
            modalConfig.OnOk = (config) =>
            {
                string userInput = inputBox.Text;
                string backInfo = userInput;
                return true;
            };

            // 显示 Modal
            Modal.open(modalConfig);
            return inputBox.Text;
        }
    }
}