using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDesktopManager
{
    public partial class ConnectConfigForm : Form
    {
        public AccountItem ItemInfo{ private set; get; }

        public ConnectConfigForm(AccountItem item)
        {
            InitializeComponent();
            ItemInfo = item;
            if(ItemInfo == null)
            {
                ItemInfo = new AccountItem();
                ItemInfo.port = 3389;
            }
        }

        private void ConnectConfigForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = ItemInfo.accountName;
            textBox2.Text = ItemInfo.address;
            textBox3.Text = Convert.ToString(ItemInfo.port);
            textBox4.Text = ItemInfo.loginname;
            textBox5.Text = ItemInfo.password;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ItemInfo.accountName = textBox1.Text;
            ItemInfo.address = textBox2.Text;
            ItemInfo.port = Convert.ToInt32(textBox3.Text);
            ItemInfo.loginname = textBox4.Text;
            ItemInfo.password = textBox5.Text;
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
