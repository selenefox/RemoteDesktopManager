using AxMSTSCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Resources;
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

        private ResourceManager resManager;
        public RemoteDesktopManagerFrom()
        {
            InitializeComponent();
            resManager = new ResourceManager("RemoteDesktopManager.RemoteDesktopManagerFrom", typeof(RemoteDesktopManagerFrom).Assembly);
        }

        private void RemoteDesktopManagerFrom_Load(object sender, EventArgs e)
        {
            UpdateListViewUI();

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
            UpdateListViewUI();
        }

        private void UpdateListViewUI()
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
            form.Name = string.Format("form_rdp_{0}_{1}",serverIP.Replace(".","_"), DateTime.Now.Second.ToString());
            form.Text = title;
            form.Size = new Size(1024, 768);
            form.Resize += new System.EventHandler(this.RDPForm_Resize);
            form.Closing += new CancelEventHandler(this.RDPForm_Closing);

            Rectangle ScreenArea = Screen.PrimaryScreen.Bounds;
            AxMsRdpClient7NotSafeForScripting axMsRdpc = new AxMsRdpClient7NotSafeForScripting();
            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).BeginInit();
            axMsRdpc.Dock = DockStyle.Fill;
            axMsRdpc.Enabled = true;
            axMsRdpc.Name = string.Format("axMsRdpc_{0}_{1}", serverIP.Replace(".", "_"), DateTime.Now.Second.ToString());

            // bind rdp connect's events
            axMsRdpc.OnDisconnected += RDC_Event_OnDisconnected;
            axMsRdpc.OnLeaveFullScreenMode += RDC_Event_OnLeaveFullScreenMode;
            axMsRdpc.OnEnterFullScreenMode += RDC_Event_OnEnterFullScreenMode;

            form.Controls.Add(axMsRdpc);
            form.Show();
            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).EndInit();

            axMsRdpc.Server = serverIP;
            axMsRdpc.UserName = loginname;
            axMsRdpc.AdvancedSettings7.RDPPort = Convert.ToInt32(serverPort);
            //axMsRdpc.AdvancedSettings9.SmartSizing = true;
            axMsRdpc.AdvancedSettings7.EnableCredSspSupport = true;
            axMsRdpc.AdvancedSettings7.ClearTextPassword = password;
            axMsRdpc.ColorDepth = 32;
            axMsRdpc.FullScreen = true;
            axMsRdpc.DesktopWidth = ScreenArea.Width;
            axMsRdpc.DesktopHeight = ScreenArea.Height;
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

                    var re = MessageBox.Show(resManager.GetString("delete.messagebox.caption"), resManager.GetString("delete.messagebox.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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

        private void popConnectMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                RDMListViewItem item = listView1.SelectedItems[0] as RDMListViewItem;
                connectRemoteDesktop(item.ItemInfo.loginname, item.ItemInfo.password, item.ItemInfo.address, item.ItemInfo.port, item.ItemInfo.accountName);
            }
        }

        private void RemoteDesktopManagerFrom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(form2rdpMap.Count > 0)
            {
                var re = MessageBox.Show(resManager.GetString("quit.messagebox.cation"), resManager.GetString("quit.messagebox.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
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
