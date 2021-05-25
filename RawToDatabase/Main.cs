using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Net;
using System.IO;
using System.Threading;
using System.Media;

namespace RawToDatabase
{
    public partial class Main : Form
    {
        OdbcConnection conn = new OdbcConnection();
        StringBuilder globalcsv;
        Boolean headerWrite = false;

        public Main()
        {
            InitializeComponent();
        }

        private void btnaddrow_Click(object sender, EventArgs e)
        {
            ofd.Filter = "EJ data | *.dat";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String sFileName = ofd.FileName; //get path
                String[] ofdSelectedFiles = ofd.SafeFileNames;
                dgvPath.Columns.Add("path", "rawdatapath");
                dgvPath.Columns[0].Width = 700;

                String _path = sFileName.Substring(0, sFileName.IndexOf(ofdSelectedFiles[0]));
                foreach (string fontFileNames in ofdSelectedFiles)
                {
                    //allString = allString + fontFileNames;
                    dgvPath.Rows.Add(_path + fontFileNames);
                }
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            conn.ConnectionString = "Driver={SQL Server};Server=172.16.1.244;DataBase=rawdata;Uid=sa;Pwd=hanaatm2014!;";
            AddCmbTableConfigId(conn);

        }


        private void AddCmbTableConfigId(OdbcConnection _odbccon)
        {
            /*DataTable objDt = new DataTable();
            OdbcConnection odbccon = _odbccon;
            try
            {
                _odbccon.Open();
                OdbcCommand cmd = new OdbcCommand("{call getTableName(?)}", _odbccon);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@pc_parameter", "Cabang");
                //cmd.Parameters.AddWithValue("@pc_key", "");
                cmd.ExecuteNonQuery();
                using (OdbcDataAdapter adapter = new OdbcDataAdapter(cmd))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dtResult = new DataSet();
                    dtResult.Tables.Add(dataTable);
                }
                this.cmbtablename.DataSource = dtResult.Tables[0];
                cmbtablename.DisplayMember = "TABLE_NAME";
                cmbtablename.ValueMember = "TABLE_NAME";
            }
            catch (OdbcException objEx) { string str = objEx.Message; }
            finally { odbccon.Close(); }*/
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            String _table = "PRIMA";//cmbtablename.Text;
            String _path = null;
            headerWrite = false;
            Boolean EjCrm = false;

            int _rowCount = dgvPath.Rows.Count;
            int _rowPosition = 0;
            //check EJ crm or not
            foreach (DataGridViewRow rowAlert in dgvPath.Rows)
            {
                if (_rowPosition < _rowCount - 1)
                {
                    _path = rowAlert.Cells["path"].Value.ToString();
                    StreamReader srFileRead = File.OpenText(_path);
                    EjCrm = JournalToNewLineString(_table, _path, "<== Transaction End").ToString().Contains("Terminal Id: 022");
                    break;
                }
            }

            if (_rowCount < 1) MessageBox.Show("\nPlease check if EJ report already add!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            else if (EjCrm == true)
            { //EJ for CRM
                globalcsv = new StringBuilder();
                try
                {
                    foreach (DataGridViewRow rowAlert in dgvPath.Rows)
                    {
                        if (_rowPosition < _rowCount - 1)
                        {
                            _path = rowAlert.Cells["path"].Value.ToString();
                            WritePath(_path, JournalToNewLineStringLongString(JournalToNewLineString(_table, _path, "<== Transaction End"), "Receipt Printer Error"));
                            NewLineStringToDataCRM(_table, _path.Replace(".dat", "_.txt"));
                        }
                    }
                }
                catch
                {
                    if (chkJoin.Checked)
                    {
                        //File.WriteAllText(_path.Replace(".txt", ".csv"), globalcsv.ToString());
                        SaveFileDialog savefile = new SaveFileDialog();
                        savefile.FileName = "Transaksi CRM.csv";
                        savefile.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";

                        if (savefile.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllText(savefile.FileName, globalcsv.ToString());
                            //using (StreamWriter sw = new StreamWriter(savefile.FileName)) sw.WriteLine("Hello World!");
                        }
                    }
                }
                MessageBox.Show("Complete!\nPlease check CSV file with the same name for report", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else //EJ for ATM
            {
                globalcsv = new StringBuilder();
                try
                {
                    foreach (DataGridViewRow rowAlert in dgvPath.Rows)
                    {
                        if (_rowPosition < _rowCount - 1)
                        {
                            _path = rowAlert.Cells["path"].Value.ToString();
                            WritePath(_path, JournalToNewLineString(_table, _path, "TRANSACTION_COMPLETE"));
                            NewLineStringToData(_table, _path.Replace(".dat", "_.txt"));
                        }
                    }
                }
                catch
                {
                    if (chkJoin.Checked)
                    {
                        //File.WriteAllText(_path.Replace(".txt", ".csv"), globalcsv.ToString());
                        SaveFileDialog savefile = new SaveFileDialog();
                        savefile.FileName = "Transaksi ATM.csv";
                        savefile.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";

                        if (savefile.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllText(savefile.FileName, globalcsv.ToString());
                            //using (StreamWriter sw = new StreamWriter(savefile.FileName)) sw.WriteLine("Hello World!");
                        }
                    }
                }
                MessageBox.Show("Complete!\nPlease check CSV file with the same name for report", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private String JournalToNewLineString(String _table, String _path, String _strReplace)
        {
            StreamReader srFile = File.OpenText(_path);

            string readCurrentLine = "";
            String LongString = "";
            String SplitedLongString = "";

            var lineCount = 0;
            using (var reader = File.OpenText(@_path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }
            while ((readCurrentLine = srFile.ReadLine()) != null)
            {
                try
                {
                    LongString = LongString + readCurrentLine;
                }
                catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
            }
            return SplitedLongString = LongString.Replace(_strReplace, _strReplace + System.Environment.NewLine);
        }


        private String JournalToNewLineStringLongString(String _longString, String _strReplace)
        {
            String LongString = "";
            String SplitedLongString = "";

            LongString = _longString;
            return SplitedLongString = LongString.Replace(_strReplace, _strReplace + System.Environment.NewLine);
        }



        private void NewLineStringToData(String _table, String _path)
        {
            StreamReader srFile = File.OpenText(_path);

            string readCurrentLine = "";
            String LongString = "";
            String SplitedLongString = "";
            String _datetime, _atmid, _kartu, _reference, _amount;
            //before your loop
            string _tmpdatetime, _tmpatmid, _tmpkartu;
            _tmpdatetime = ""; _tmpatmid = ""; _tmpkartu = "";

            var globallineCount = 0;

            var csv = new StringBuilder();
            var lineCount = 0;
            var newHeaderLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", "DATE", "TIME", "ATM ID", "NO KARTU", "STAN", "TRANSAKSI", "JUMLAH TRANSAKSI", "PECAHAN 50", "PECAHAN 100");
            csv.AppendLine(newHeaderLine);
            if (chkJoin.Checked & headerWrite == false) globalcsv.AppendLine(newHeaderLine); //(x)
            headerWrite = true;

            using (var reader = File.OpenText(@_path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }
            /*while ((readCurrentLine = srFile.ReadLine()) != null)
            {
                try
                {
                    if (readCurrentLine.Contains("PENARIKAN")) {
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 24, 24).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") + 11, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            if (readCurrentLine.Contains("N0. URUT  :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("N0. URUT  :") + 11, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT  :") + 11, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("JUMLAH    :") + 11, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Trim();
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""));
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("WIDTHDRAWAL"))
                    {
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :"); 
                        if (startIndex>0) {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 24, 24).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""));
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("{0} Exception caught.", e); }
            }*/

            //=========================================
            int counter = 0;
            string line;

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(_path);
            while ((readCurrentLine = file.ReadLine()) != null)
            {
                try
                {
                    if (readCurrentLine.Contains("PENARIKAN"))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70,100));
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 24, 24).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") + 11, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            if (readCurrentLine.Contains("N0. URUT  :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("N0. URUT  :") + 11, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT  :") + 11, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("JUMLAH    :") + 11, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Trim();
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "ID");
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("WIDTHDRAWAL"))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70, 100));
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 24, 24).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                        else if (readCurrentLine.Contains("ATM ID      :"))
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 24, 24).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }
                counter++;
            }

            file.Close();
            //=========================================

            //after your loop
            try
            {
                if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt", ".csv"), csv.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e); MessageBox.Show("Error while writing csv file, \nmake sure you not open the csv file in exel!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void NewLineStringToDataCRM(String _table, String _path)
        {
            StreamReader srFile = File.OpenText(_path);

            string _tmpdatetime, _tmpatmid, _tmpkartu;
            _tmpdatetime = ""; _tmpatmid = ""; _tmpkartu = "";

            string readCurrentLine = "";
            String LongString = "";
            String SplitedLongString = "";
            String _datetime, _atmid, _kartu, _reference, _amount, _amount50, _amount100, _tmpamount;
            //before your loop
            _datetime = ""; _atmid = ""; _kartu = "";
            var globallineCount = 0;

            var csv = new StringBuilder();
            var lineCount = 0;
            var newHeaderLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", "DATE", "TIME", "ATM ID", "NO KARTU", "STAN", "TRANSAKSI", "JUMLAH TRANSAKSI", "PECAHAN 50", "PECAHAN 100");
            csv.AppendLine(newHeaderLine);
            if (chkJoin.Checked & headerWrite == false) globalcsv.AppendLine(newHeaderLine); //(x)
            headerWrite = true;

            using (var reader = File.OpenText(@_path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }

            int counter = 0;
            string line;

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(_path);
            while ((readCurrentLine = file.ReadLine()) != null)
            {
                try
                {
                    if (readCurrentLine.Contains("Host Store: Stored"))
                    {

                        try
                        {
                            
                            _tmpdatetime = _datetime; _tmpatmid = _atmid; _tmpkartu = _kartu;
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Terminal Id: ") - 20, 20).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Terminal Id: ") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 13, 16).Trim();
                        }
                        catch {
                            _datetime = _tmpdatetime; _atmid = _tmpatmid = _atmid; _atmid = _tmpkartu;
                        }
                        _reference = "-";
                        _tmpamount = readCurrentLine.Substring(readCurrentLine.IndexOf("Host Store: Stored") + 18 + 3, 20).Trim();
                        String[] _amountSplit1 = _tmpamount.Split('I');
                        _amount = "";
                        _amount50 = ""; _amount100 = ""; //IDR50000:14 IDR100000:10
                        int i = 1;
                        if (_tmpamount.Contains("50000") && _tmpamount.Contains("100000"))
                        {
                            foreach (String word in _amountSplit1)
                            {
                                string[] _amountSplit2 = word.Split(':');
                                foreach (String word2 in _amountSplit2)
                                {
                                    if (word2.Contains("50000")) { _amount50 = _amountSplit2[1].ToString(); break; }
                                    if (word2.Contains("100000")) { _amount100 = _amountSplit2[1].ToString(); break; }
                                }
                            }
                        }
                        else if (_tmpamount.Contains("100000") && _tmpamount.Contains("50000") == false)
                        {
                            foreach (String word in _amountSplit1)
                            {
                                String[] _amountSplit2 = word.Split(':');
                                int a = word.ToString().IndexOf(':');
                                int b = word.ToString().IndexOf('A');
                                foreach (String word2 in _amountSplit2)
                                {

                                    if (Int32.Parse(word2) == 50000) { _amount50 = "0"; }
                                    if (Int32.Parse(word2) == 100000) { _amount100 = word.Substring(a + 1, b - a - 1); break; }
                                }
                            }
                        }
                        else if (_tmpamount.Contains("50000") && _tmpamount.Contains("100000") == false)
                        {
                            foreach (String word in _amountSplit1)
                            {
                                String[] _amountSplit2 = word.Split(':');
                                int a = word.ToString().IndexOf(':');
                                int b = word.ToString().IndexOf('A');
                                foreach (string word2 in _amountSplit2)
                                {
                                    if (Int32.Parse(word2) == 50000) { _amount50 = word.Substring(a + 1, b - a - 1); break; }
                                    if (Int32.Parse(word2) == 100000) { _amount100 = "0"; }
                                }
                            }
                        }
                        //MessageBox.Show("50-100\n" + string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference,"DEPOSIT", _amount, _amount50, _amount100));
                        //Int32.TryParse("-105", out j)
                        var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, "'" + _kartu, _reference, "DEPOSIT", _amount, _amount50, _amount100);
                        csv.AppendLine(newLine);
                        if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                    }
                    if (readCurrentLine.Contains("Notes Dispensed:"))
                    {
                        _amount = "";   
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Terminal Id: ") - 20, 20).Trim();
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Terminal Id: ") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 13, 16).Trim();
                            _reference = "-";
                            _amount50 = "0";
                            _amount100 = "0";

                            if (readCurrentLine.Contains("[OP Code]: [CA   I C]")) { _amount = "100000"; }
                            if (readCurrentLine.Contains("[OP Code]: [CA   H C]")) { _amount = "500000"; }
                            if (readCurrentLine.Contains("[OP Code]: [CA   G C]")) { _amount = "1300000"; }
                            if (readCurrentLine.Contains("[OP Code]: [CA   C C]")) { _amount = "1500000"; }
                            if (readCurrentLine.Contains("[OP Code]: [CA   B C]")) { _amount = "1000000"; }
                            if (readCurrentLine.Contains("[OP Code]: [CA   A C]")) { _amount = "300000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   I C]")) { _amount = "100000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   H C]")) { _amount = "500000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   C C]")) { _amount = "1300000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   C C]")) { _amount = "1500000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   B C]")) { _amount = "1000000"; }
                            if (readCurrentLine.Contains("[OP Code]: [BA   A C]")) { _amount = "300000"; }


                            string AB = readCurrentLine.Substring(readCurrentLine.IndexOf("Total Amount") + 17, 8).Trim();
                            if (_amount == "")  _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("Total Amount") + 17, readCurrentLine.Substring(readCurrentLine.IndexOf("Total Amount") + 17, 8).IndexOf('A')).Trim();

                            string a, b, c, d, e, f, g, h;
                            a = _datetime.Substring(0, 11).Trim();
                            b = _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", "");
                            c = _atmid;
                            d = _kartu;
                            e = _reference;
                            f = _amount;
                            g = _amount50;
                            h = _amount100;
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, "'" + _kartu, _reference, "WITHDRAW", _amount, _amount50, _amount100);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }
                counter++;
            }

            file.Close();
            //=========================================

            //after your loop
            try
            {
                if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt", ".csv"), csv.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e); MessageBox.Show("Error while writing csv file, \nmake sure you not open the csv file in exel!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool TruncateTable(OdbcConnection _odbccon)
        {
            DataTable objDt = new DataTable();
            OdbcConnection odbccon = _odbccon;
            try
            {
                _odbccon.Open();
                OdbcCommand cmd = new OdbcCommand("{call truncateTable(?,?)}", _odbccon);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@pc_nik", "");
                cmd.Parameters.AddWithValue("@pc_password", "");
                cmd.ExecuteNonQuery();
            }
            catch (OdbcException objEx) { string str = objEx.Message; }
            finally { odbccon.Close(); }
            return true;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker1 = sender as BackgroundWorker;
            for (int j = 0; j < 100000; j++)
            {
                Calculate(j);
                backgroundWorker1.ReportProgress((j * 100) / 100000);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // TODO: do something with final calculation.
        }

        private void Calculate(int i)
        {
            double pow = Math.Pow(i, i);
        }

        private void WritePath(String _path, String _content)
        {
            String _newpath = _path.Replace(".dat", "_.txt");
            // This text is added only once to the file.
            if (!File.Exists(_newpath))
            {
                File.WriteAllText(_newpath, _content);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            var myForm = new Help();
            myForm.Show();
        }

    }
}
