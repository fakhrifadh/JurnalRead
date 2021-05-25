using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RawToDatabase
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Help_Load(object sender, EventArgs e)
        {
            richTextBox1.AppendText("Aplikasi ini untuk mengekstrak data jurnal yang ada di ATM.");
            richTextBox1.AppendText(Environment.NewLine + "File ini dapat mengekstrak satu file atau beberapa file secara bersamaan.");
            richTextBox1.AppendText(Environment.NewLine + "==Ekstrak data perfile");
            richTextBox1.AppendText(Environment.NewLine + "1. tambahkan file yang akan di ekstrak dengan klik tombol add (bisa  satu persatu atau banyak sekaligus)");
            richTextBox1.AppendText(Environment.NewLine + "2. klik tombol execute");
            richTextBox1.AppendText(Environment.NewLine + "3. tunggu hingga ada notifikasi selesai");
            richTextBox1.AppendText(Environment.NewLine + "4. file akan terbentuk otomatis di direktori yang sama dengan extention .CSV");
            richTextBox1.AppendText(Environment.NewLine + Environment.NewLine + "==Ekstrak data batch bersamaan");
            richTextBox1.AppendText(Environment.NewLine + "1. tambahkan file yang akan di ekstrak secara banyak sekaligus dengan klik tombol add");
            richTextBox1.AppendText(Environment.NewLine + "2. cek list join satu file");
            richTextBox1.AppendText(Environment.NewLine + "3. klik tombol execute");
            richTextBox1.AppendText(Environment.NewLine + "4. tunggu hingga ada notifikasi selesai");
            richTextBox1.AppendText(Environment.NewLine + "5. muncul save dialog yang meminta nama file .CSV yang akan disimpan, nama default penyimpanan adalah Transaksi ATM.csv");
            richTextBox1.AppendText(Environment.NewLine + Environment.NewLine + "==Ekstrak data batch bersamaan");
            richTextBox1.AppendText(Environment.NewLine + "Copyright R&D 2017"); 
        }
    }
}
