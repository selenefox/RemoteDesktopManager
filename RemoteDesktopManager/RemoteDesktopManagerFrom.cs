using AxMSTSCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDesktopManager
{
    public partial class RemoteDesktopManagerFrom : Form
    {
        private static string CONFIG_FILENAME = "rdm.json";
        private List<AccountItem> dataList = new List<AccountItem>();
        private Dictionary<string, Form> rdp2formMap= new Dictionary<string, Form>();
        private Dictionary<string, AxMsRdpClient7NotSafeForScripting> form2rdpMap = new Dictionary<string, AxMsRdpClient7NotSafeForScripting>();

        public RemoteDesktopManagerFrom()
        {
            InitializeComponent();
        }

        private void RemoteDesktopManagerFrom_Load(object sender, EventArgs e)
        {
            updateListViewUI();

            // load config.json
            AccountItem[] datas = JSONUtils.Parse(CONFIG_FILENAME);
            Trace.TraceInformation("rdm.json - data count:{0}", datas.Length);
            listView1.BeginUpdate();
            foreach (AccountItem item in datas){
                listView1.Items.Add(new RDMListViewItem().BindData(item));
                dataList.Add(item);
            }
            listView1.EndUpdate();
            editMenuItem.Enabled = false;
            deleteMenuItem.Enabled = false;
        }

        private void RemoteDesktopManagerFrom_Resize(object sender, EventArgs e)
        {
            updateListViewUI();
        }

        private void updateListViewUI()
        {
            columnHeader1.Width = this.Width / 10 * 3;
            columnHeader2.Width = this.Width / 10 * 3;
            columnHeader3.Width = this.Width / 10 * 2;
            columnHeader4.Width = this.Width / 10 * 2 - 20;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(listView1.SelectedItems.Count == 1)
            {
                RDMListViewItem item = listView1.SelectedItems[0] as RDMListViewItem;
                connectRemoteDesktop(item.ItemInfo.loginname, item.ItemInfo.password, item.ItemInfo.address, item.ItemInfo.port, item.ItemInfo.accountName);
            }
        }

        private void connectRemoteDesktop(string loginname, string password, string serverIP, int serverPort = 3389, string title = "")
        {
            Form form = new Form();
            form.ShowIcon = false;
            form.Name = string.Format("form_rdp_dlg_{0}",serverIP.Replace(".","_"));
            form.Text = title;
            // 窗口大小为 1024 * 768
            form.Size = new Size(1024, 768);
            form.Resize += new System.EventHandler(this.RDPForm_Resize);
            form.Closing += new CancelEventHandler(this.RDPForm_Closing);
            // 获取屏幕尺寸
            Rectangle ScreenArea = Screen.PrimaryScreen.Bounds;
            // 创建RdpClient
            AxMsRdpClient7NotSafeForScripting axMsRdpc = new AxMsRdpClient7NotSafeForScripting();
            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).BeginInit();
            axMsRdpc.Dock = DockStyle.Fill;
            axMsRdpc.Enabled = true;
            axMsRdpc.Name = string.Format("axMsRdpc_{0}", title);

            // 绑定连接与释放事件
            axMsRdpc.OnDisconnected += RDC_Event_OnDisconnected;
            axMsRdpc.OnLeaveFullScreenMode += RDC_Event_OnLeaveFullScreenMode;
            axMsRdpc.OnEnterFullScreenMode += RDC_Event_OnEnterFullScreenMode;

            // 将COM组件Rdpc添加到新窗口中
            form.Controls.Add(axMsRdpc);
            // 打开新窗口
            form.Show();
            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).EndInit();

            // 服务器地址
            axMsRdpc.Server = serverIP;
            // 远程登录账号
            axMsRdpc.UserName = loginname;
            // 远程端口号
            axMsRdpc.AdvancedSettings7.RDPPort = Convert.ToInt32(serverPort);
            // 自动控制屏幕显示尺寸
            //axMsRdpc.AdvancedSettings9.SmartSizing = true;
            // 启用CredSSP身份验证（有些服务器连接没有反应，需要开启这个）
            axMsRdpc.AdvancedSettings7.EnableCredSspSupport = true;
            // 远程登录密码
            axMsRdpc.AdvancedSettings7.ClearTextPassword = password;
            // 颜色位数 8,16,24,32
            axMsRdpc.ColorDepth = 32;
            // 开启全屏 true|flase
            axMsRdpc.FullScreen = true;
            // 设置远程桌面宽度为显示器宽度
            axMsRdpc.DesktopWidth = ScreenArea.Width;
            // 设置远程桌面宽度为显示器高度
            axMsRdpc.DesktopHeight = ScreenArea.Height;
            // 远程连接
            axMsRdpc.Connect();
            rdp2formMap.Add(axMsRdpc.Name, form);
            form2rdpMap.Add(form.Name, axMsRdpc);
        }

        private void RDC_Event_OnEnterFullScreenMode(object sender, EventArgs e)
        {
            AxMsRdpClient7NotSafeForScripting rdc = sender as AxMsRdpClient7NotSafeForScripting;
            if (rdp2formMap.ContainsKey(rdc.Name))
            {
                Form form = rdp2formMap[rdc.Name];
                form.WindowState = FormWindowState.Minimized;
            }
        }

        private void RDC_Event_OnLeaveFullScreenMode(object sender, EventArgs e)
        {
            AxMsRdpClient7NotSafeForScripting rdc = sender as AxMsRdpClient7NotSafeForScripting;
            if (rdp2formMap.ContainsKey(rdc.Name))
            {
                Form form = rdp2formMap[rdc.Name];
                if (form.WindowState != FormWindowState.Normal)
                {
                    form.WindowState = FormWindowState.Normal;
                }

                if (form.Location.Y < 0)
                {
                    form.Location = new Point(form.Location.X, 10);
                }
                if (form.Location.X < 0)
                {
                    form.Location = new Point(10, form.Location.Y);
                }
            }
        }

        private void RDC_Event_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            AxMsRdpClient7NotSafeForScripting rdc = sender as AxMsRdpClient7NotSafeForScripting;
            if (rdp2formMap.ContainsKey(rdc.Name))
            {
                Form form = rdp2formMap[rdc.Name];
                if (form2rdpMap.ContainsKey(form.Name))
                {
                    form2rdpMap.Remove(form.Name);
                }
                form.Close();
                rdp2formMap.Remove(rdc.Name);
                Trace.TraceInformation("rdm session closed.");
            }
        }

        private void RDPForm_Resize(object sender, EventArgs e)
        {
            Form ThisForm = sender as Form;
            Trace.TraceInformation("RDPForm_Resize" + ThisForm.WindowState.ToString() + e.ToString());
            if(form2rdpMap.ContainsKey(ThisForm.Name))
            {
                if (ThisForm.WindowState == FormWindowState.Maximized)
                {

                    form2rdpMap[ThisForm.Name].FullScreen = false;
                    form2rdpMap[ThisForm.Name].FullScreen = true;
                }
                else if (ThisForm.WindowState == FormWindowState.Normal)
                {
                    form2rdpMap[ThisForm.Name].FullScreen = false;
                }
            }
        }

        private void RDPForm_Closing(object sender, CancelEventArgs e)
        {
            Form ThisForm = sender as Form;
            if (form2rdpMap.ContainsKey(ThisForm.Name))
            {
                form2rdpMap[ThisForm.Name].Disconnect();
                e.Cancel = true;
            }
        }

        private void createMenuItem_Click(object sender, EventArgs e)
        {
            ConnectConfigForm configForm = new ConnectConfigForm(null);
            if(configForm.ShowDialog() == DialogResult.OK)
            {
                dataList.Add(configForm.ItemInfo);
                listView1.BeginUpdate();
                listView1.Items.Add(new RDMListViewItem().BindData(configForm.ItemInfo));
                listView1.EndUpdate();

                JSONUtils.Write(dataList.ToArray(), CONFIG_FILENAME);
            }

        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            ListViewItem item = this.listView1.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                editMenuItem.Enabled = true;
                deleteMenuItem.Enabled = true;
                if(e.Button == MouseButtons.Right)
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
            else
            {
                editMenuItem.Enabled = false;
                deleteMenuItem.Enabled = false;
            }
        }

        private void editMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                try
                {
                    RDMListViewItem listViewItem = listView1.SelectedItems[0] as RDMListViewItem;
                    AccountItem itemData = dataList[listViewItem.Index];

                    ConnectConfigForm configForm = new ConnectConfigForm(itemData);
                    configForm.ShowDialog();
                    if(configForm.DialogResult == DialogResult.OK)
                    {
                        listViewItem.Refresh();
                        JSONUtils.Write(dataList.ToArray(), CONFIG_FILENAME);
                    }
                }
                catch { }
            }
        }

        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                try
                {
                    RDMListViewItem listViewItem = listView1.SelectedItems[0] as RDMListViewItem;
                    AccountItem itemData = dataList[listViewItem.Index];

                    var re = MessageBox.Show("此操作不可撤销，是否确认删除该记录？", "删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if(re == DialogResult.Yes)
                    {
                        dataList.Remove(itemData);
                        listView1.Items.Remove(listViewItem);
                        JSONUtils.Write(dataList.ToArray(), CONFIG_FILENAME);
                    }
                }
                catch { }
            }
        }

        private void RemoteDesktopManagerFrom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(form2rdpMap.Count > 0)
            {
                var re = MessageBox.Show("当前有连接中的远程桌面，是否关闭全部连接并退出程序？", "退出确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if(re == DialogResult.Yes)
                {
                    foreach (KeyValuePair<string, AxMsRdpClient7NotSafeForScripting> kv in form2rdpMap)
                    {
                        kv.Value.Disconnect();
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }

    public class RDMListViewItem: ListViewItem
    {
        public AccountItem ItemInfo { get; private set; }

        public RDMListViewItem BindData(AccountItem iteminfo)
        {
            ItemInfo = iteminfo;
            this.Text = ItemInfo.accountName;
            this.SubItems.Add(ItemInfo.address);
            this.SubItems.Add(Convert.ToString(ItemInfo.port));
            this.SubItems.Add(ItemInfo.loginname);
            return this;
        }

        public void Refresh(AccountItem iteminfo = null)
        {
            if(iteminfo != null)
            {
                ItemInfo = iteminfo;
            }
            this.Text = ItemInfo.accountName;
            this.SubItems[1].Text= ItemInfo.address;
            this.SubItems[2].Text = Convert.ToString(ItemInfo.port);
            this.SubItems[3].Text = ItemInfo.loginname;
        }
    }
}
