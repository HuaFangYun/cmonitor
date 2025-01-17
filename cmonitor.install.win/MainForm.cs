using common.libs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;

namespace cmonitor.install.win
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            keyboardIndex.ReadOnly = true;
            wallpaperIndex.ReadOnly = true;
            llockIndex.ReadOnly = true;
            sasIndex.ReadOnly = true;

            LoadConfig();
            SaveConfig();

            CheckInstall();
            CheckRunning();
        }

        private void SaveConfig()
        {
            RegistryKey key = CheckRegistryKey();
            key.SetValue("modeClient", modeClient.Checked ? "1" : "0");
            key.SetValue("modeServer", modeServer.Checked ? "1" : "0");

            key.SetValue("machineName", machineName.Text);

            key.SetValue("serverIP", serverIP.Text);
            key.SetValue("serverPort", serverPort.Text);
            key.SetValue("apiPort", apiPort.Text);
            key.SetValue("webPort", webPort.Text);

            key.SetValue("reportDelay", reportDelay.Text);
            key.SetValue("screenDelay", screenDelay.Text);
            key.SetValue("screenScale", screenScale.Text);

            key.SetValue("shareKey", shareKey.Text);
            key.SetValue("shareLen", shareLen.Text);
            key.SetValue("shareItemLen", shareItemLen.Text);

            key.SetValue("keyboardIndex", keyboardIndex.Text);
            key.SetValue("wallpaperIndex", wallpaperIndex.Text);
            key.SetValue("llockIndex", llockIndex.Text);
            key.SetValue("sasIndex", sasIndex.Text);


        }
        private void LoadConfig()
        {
            RegistryKey key = CheckRegistryKey();

            string hostname = Dns.GetHostName();

            modeClient.Checked = key.GetValue("modeClient", "0").ToString() == "1";
            modeServer.Checked = key.GetValue("modeServer", "0").ToString() == "1";

            machineName.Text = key.GetValue("machineName", hostname).ToString();

            serverIP.Text = key.GetValue("serverIP", "127.0.0.1").ToString();
            serverPort.Text = key.GetValue("serverPort", "1802").ToString();
            apiPort.Text = key.GetValue("apiPort", "1801").ToString();
            webPort.Text = key.GetValue("webPort", "1800").ToString();

            reportDelay.Text = key.GetValue("reportDelay", "30").ToString();
            screenDelay.Text = key.GetValue("screenDelay", "200").ToString();
            screenScale.Text = key.GetValue("screenScale", "0.2").ToString();

            shareKey.Text = key.GetValue("shareKey", "cmonitor/share").ToString();
            shareLen.Text = key.GetValue("shareLen", "10").ToString();
            shareItemLen.Text = key.GetValue("shareItemLen", "1024").ToString();

            keyboardIndex.Text = "1";
            wallpaperIndex.Text = "2";
            llockIndex.Text = "3";
            sasIndex.Text = "4";
        }

        private RegistryKey CheckRegistryKey()
        {
            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\cmonitor", "test", 1);

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software");
            return key.OpenSubKey("cmonitor", true);
        }


        bool loading = false;
        bool installed = false;
        bool running = false;
        string serviceName = "cmonitor.sas.service";
        string exeName = "cmonitor.sas.service.exe";
        private void OnInstallClick(object sender, EventArgs e)
        {
            if (loading)
            {
                return;
            }

            List<string> installParams = new List<string>();

            bool result = CheckMode(installParams);
            if (result == false) return;
            result = CheckIPAndPort(installParams);
            if (result == false) return;
            result = CheckDelay(installParams);
            if (result == false) return;
            result = CheckShare(installParams);
            if (result == false) return;

            CheckLoading(true);
            SaveConfig();

            string paramStr = string.Join(" ", installParams);

            string filename = Process.GetCurrentProcess().MainModule.FileName;
            string dir = Path.GetDirectoryName(filename);
            string sasPath = Path.Combine(dir, exeName);

            string sasIndexStr = sasIndex.Text;

            string shareKeyStr = shareKey.Text;
            string shareLenStr = shareLen.Text;

            Task.Run(() =>
            {
                if (installed == false)
                {
                    string taskStr = $"sc create \"{serviceName}\" binpath=\"{sasPath} {shareKeyStr} {shareLenStr} 255 {sasIndexStr} \\\"{paramStr}\\\"\" start=AUTO";
                    CommandHelper.Windows(string.Empty, new string[] {
                        taskStr,
                        $"net start {serviceName}",
                    });
                }
                else
                {
                    while (running)
                    {
                        Stop();
                        System.Threading.Thread.Sleep(1000);
                        CheckRunning();
                    }
                    string resultStr = CommandHelper.Windows(string.Empty, new string[] {
                        "schtasks /delete /TN \"cmonitorService\" /f",
                        $"net stop {serviceName}",
                        $"sc delete {serviceName}",
                    });
                }

                CheckLoading(false);
                CheckInstall();
                CheckRunning();
            });
        }

        private bool CheckMode(List<string> installParams)
        {
            if (modeClient.Checked == false && modeServer.Checked == false)
            {
                MessageBox.Show("客户端和服务端必须选择一样！");
                return false;
            }
            List<string> modeStr = new List<string>();
            if (modeClient.Checked)
            {
                modeStr.Add("client");
            }
            if (modeServer.Checked)
            {
                modeStr.Add("server");
            }
            installParams.Add($"--mode {string.Join(",", modeStr)}");

            return true;
        }
        private bool CheckIPAndPort(List<string> installParams)
        {
            if (string.IsNullOrWhiteSpace(serverIP.Text))
            {
                MessageBox.Show("服务器ip必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(serverPort.Text))
            {
                MessageBox.Show("服务器端口必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(apiPort.Text))
            {
                MessageBox.Show("管理端口必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(webPort.Text))
            {
                MessageBox.Show("web端口必填");
                return false;
            }
            installParams.Add($"--server {serverIP.Text}");
            installParams.Add($"--service {serverPort.Text}");
            installParams.Add($"--api {apiPort.Text}");
            installParams.Add($"--web {webPort.Text}");

            return true;
        }
        private bool CheckDelay(List<string> installParams)
        {
            if (string.IsNullOrWhiteSpace(reportDelay.Text))
            {
                MessageBox.Show("报告间隔时间必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(screenDelay.Text))
            {
                MessageBox.Show("截屏间隔时间必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(screenScale.Text))
            {
                MessageBox.Show("截屏缩放比例必填");
                return false;
            }
            installParams.Add($"--report-delay {reportDelay.Text}");
            installParams.Add($"--screen-delay {screenDelay.Text}");
            installParams.Add($"--screen-scale {screenScale.Text}");

            return true;
        }
        private bool CheckShare(List<string> installParams)
        {
            if (string.IsNullOrWhiteSpace(shareKey.Text))
            {
                MessageBox.Show("共享数据键必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(shareLen.Text))
            {
                MessageBox.Show("共享数量必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(shareItemLen.Text))
            {
                MessageBox.Show("共享每项数据长度必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(keyboardIndex.Text))
            {
                MessageBox.Show("键盘键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(wallpaperIndex.Text))
            {
                MessageBox.Show("壁纸键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(llockIndex.Text))
            {
                MessageBox.Show("锁屏键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(sasIndex.Text))
            {
                MessageBox.Show("sas键下标必填");
                return false;
            }
            installParams.Add($"--share-key {shareKey.Text}");
            installParams.Add($"--share-len {shareLen.Text}");
            installParams.Add($"--share-item-len {shareItemLen.Text}");

            return true;
        }

        private void CheckLoading(bool state)
        {
            loading = state;
            this.Invoke(new EventHandler(delegate
            {
                if (loading)
                {
                    installBtn.Text = "操作中..";
                    runBtn.Text = "操作中..";
                    checkStateBtn.Text = "操作中..";
                }
                else
                {
                    checkStateBtn.Text = "检查状态";
                    if (installed)
                    {
                        installBtn.ForeColor = Color.Red;
                        installBtn.Text = "解除自启动";
                        runBtn.Enabled = true;
                    }
                    else
                    {
                        installBtn.ForeColor = Color.Black;
                        installBtn.Text = "安装自启动";
                        runBtn.Enabled = false;
                    }

                    if (running)
                    {
                        runBtn.ForeColor = Color.Red;
                        runBtn.Text = "停止运行";
                    }
                    else
                    {
                        runBtn.ForeColor = Color.Black;
                        runBtn.Text = "启动";
                    }
                }
            }));
        }
        private void CheckInstall()
        {
            Task.Run(() =>
            {
                string result = CommandHelper.Windows(string.Empty, new string[] { $"sc query {serviceName}" });
                installed = result.Contains($"SERVICE_NAME: {serviceName}");
                CheckLoading(loading);
            });
        }

        private void RunClick(object sender, EventArgs e)
        {
            if (loading) return;

            CheckLoading(true);

            Task.Run(() =>
            {
                if (running)
                {
                    Stop();
                    while (running)
                    {
                        CheckRunning();
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    Run();
                    for (int i = 0; i < 15 && running == false; i++)
                    {
                        CheckRunning();
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                CheckLoading(false);
            });
        }
        private void Run()
        {
            CommandHelper.Windows(string.Empty, new string[] {
                    "schtasks /run /I /TN \"cmonitorService\"",
                    $"net stop {serviceName}",
                    $"net start {serviceName}",
            });
        }
        private void Stop()
        {
            CommandHelper.Windows(string.Empty, new string[] { $"net stop \"{serviceName}\"", });
        }

        private void CheckRunning()
        {
            Task.Run(() =>
            {
                string result = CommandHelper.Windows(string.Empty, new string[] { $"sc query {serviceName}" });
                running = result.Contains(": 4  RUNNING");
                CheckLoading(loading);
            });
        }

        private void checkStateBtn_Click(object sender, EventArgs e)
        {
            if (loading) return;

            CheckLoading(loading);
            CheckInstall();
            CheckRunning();
        }
    }
}
