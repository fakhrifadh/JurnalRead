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
using System.Text.RegularExpressions;

namespace RawToDatabase
{
    public partial class Form1 : Form
    {
        OdbcConnection conn = new OdbcConnection();
        StringBuilder globalcsv;
        Boolean headerWrite = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnaddrow_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*.*)|*.*|dat files (*.dat)| *.dat|txt files (*.txt)|*.txt";
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
            conn.ConnectionString = "";
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

            int _rowCount = dgvPath.Rows.Count;
            int _rowPosition = 0;
            if (_rowCount < 1) MessageBox.Show("\nPlease check if EJ report already add!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
            {
                globalcsv = new StringBuilder();
                try
                {
                    foreach (DataGridViewRow rowAlert in dgvPath.Rows)
                    {
                        if (_rowPosition < _rowCount - 1)
                        {
                            _path = rowAlert.Cells["path"].Value.ToString();
                            if (_path.Contains(".txt")) //for MoniPlus2
                            {
                                WritePath(_path, JournalToNewLineStringTxt(_table, _path));
                                NewLineStringToDataTxt(_table, _path.Replace(".txt", ".txt_"));
                            }
                            else
                            {
                                WritePath(_path, JournalToNewLineString(_table, _path));
                                NewLineStringToData(_table, _path.Replace(".dat", ".txt_"));
                            }
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

        private String JournalToNewLineString(String _table, String _path)
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
            return SplitedLongString = LongString.Replace("TRANSACTION_COMPLETE", "TRANSACTION_COMPLETE" + System.Environment.NewLine);
        }

        private String JournalToNewLineStringTxt(String _table, String _path)
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
            //return SplitedLongString = LongString.Replace("<== Transaction End", "<== Transaction End" + System.Environment.NewLine);
            //added 20200103 split cardless transaction start
            LongString = LongString.Replace("Cardless Menu", System.Environment.NewLine + "Cardless Menu");
            //added 20200103 split cardless transaction end
            return SplitedLongString = LongString.Replace("PIN Entered:", "PIN Entered:" + System.Environment.NewLine);

        }


        private void NewLineStringToData(String _table, String _path)
        {
            StreamReader srFile = File.OpenText(_path);

            string readCurrentLine = "";
            String LongString = "";
            String SplitedLongString = "";
            String _datetime, _atmid, _kartu, _reference, _amount,_errorcode;
            //before your loop

            var globallineCount = 0;

            var csv = new StringBuilder();
            var lineCount = 0;
            var newHeaderLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", "DATE", "TIME", "ATM ID", "NO KARTU", "STAN", "TRANSAKSI", "JUMLAH TRANSAKSI", "", "PICKUP COUNT", "REMAIN COUNT");
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
                            
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 29, 29).Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") + 11, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            if (readCurrentLine.Contains("N0. URUT  :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("N0. URUT  :") + 11, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT  :") + 11, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("JUMLAH    :") + 11, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pick-up Count") + 12, 24);
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count") + 11, 24);
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "ID");
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "ID", _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("WIDTHDRAWAL") || readCurrentLine.Contains("WITHDRAWAL"))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70, 100));
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 29, 29).Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pick-up Count") + 12, 24);
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count") + 11, 24);
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN", _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                        //disable on 20191114
                        else if (readCurrentLine.Contains("ATM ID      :") && readCurrentLine.Contains("CARDLESS WITHDRAWAL"))
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = "CARDLESS";//readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("Trans SEQ number") + 18, 4).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pick-up Count") + 12, 24);
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count") + 11, 24);
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN", _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                        else if (readCurrentLine.Contains("ATM ID      :"))
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pick-up Count") + 12, 24);
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count") + 11, 24);
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN", _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                        else if (readCurrentLine.Contains("ATM ID     :"))
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID     :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID     :") + 12, 8).Trim();
                            _kartu = readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.    :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT     :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pick-up Count") + 12, 24);
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count") + 11, 24);
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN", _pickupcount_4, _remaincount_4);
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
                //if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt", ".csv"), csv.ToString());
                if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt_", ".csv"), csv.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e); MessageBox.Show("Error while writing csv file, \nmake sure you not open the csv file in exel!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NewLineStringToDataTxt(String _table, String _path)
        {
            StreamReader srFile = File.OpenText(_path);

            string readCurrentLine = "";
            String LongString = "";
            String SplitedLongString = "";
            String _datetime, _atmid, _kartu, _reference, _amount,_errorcode;
            bool _skip = false;
            //before your loop
            List<string> listEC = new List<string>()
            {
            "9411200",
            "9411300",
            "9441700",
            "9441800",
            "9441900",
            "9429900",
            "94A6100",
            "94B6100",
            "94B6300",
            "94B6500",
            "94B6600",
            "94B6700",
            "94B7100",
            "94B8100",
            "94B8600",
            "94B8700",
            "94B9100",
            "94B9200",
            "94C6100",
            "94C6200",
            "94C6500",
            "94C6600",
            "94C6700",
            "94C7100",
            "94C7200",
            "94C7300",
            "9413300",
            "9413400",
            "9425600",
            "9426100",
            "9719400",
            "971A000",
            "971A100",
            "971A200",
            "971A300",
            "971A400",
            "971A500",
              };
            var globallineCount = 0;

            var csv = new StringBuilder();
            var lineCount = 0;
            var newHeaderLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", "DATE", "TIME", "ATM ID", "NO KARTU", "STAN", "TRANSAKSI", "JUMLAH TRANSAKSI", "", "PICKUP COUNT", "REMAIN COUNT");
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
                    if (readCurrentLine.Contains("PENARIKAN"))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70,100));
                        int erIndex = readCurrentLine.IndexOf("Error Code   [9");
                        if (erIndex > 0)
                        {
                            _errorcode = readCurrentLine.Substring(readCurrentLine.IndexOf("Error Code   [9") +14 , 29).Trim();
                            string ceker = _errorcode.Substring(0, 7);
                            if (listEC.Contains(ceker))
                            {
                                _skip = true;
                            }

                        }
                        
                        int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                        if (startIndex > 0)
                        {
                            if (_skip)
                            {
                                goto SkipLine;
                            }

                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 29, 29).Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") + 11, 8).Trim();

                            /* 2011112
                             * if (readCurrentLine.IndexOf("Card Number: ") > 0)
                            {
                                _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 12, 17).Trim();
                            }
                            else _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();
                            */
                            _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();

                            if (readCurrentLine.Contains("N0. URUT  :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("N0. URUT  :") + 11, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT  :") + 11, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("JUMLAH    :") + 11, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pickup Count ") + 12, 26); //cange from 24 to 26 20200121
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count ") + 11, 26);  //20200121 change 24 to 26
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------

                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "ID 1:" + counter.ToString());
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "ID 1:" + counter.ToString(), _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);

                        SkipLine:
                            _skip = false;
                        }
                    
                    }
                        
                    else
                        if (readCurrentLine.Contains("WIDTHDRAWAL") || readCurrentLine.Contains("WITHDRAWAL"))
                        {
                            //MessageBox.Show(readCurrentLine.Substring(70, 100));
                            int startIndex = readCurrentLine.IndexOf("ATM ID    :");
                            if (startIndex > 0)
                            {
                                _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID    :") - 29, 29).Trim(); //20191031 change from 24 to 29
                                _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                                if (readCurrentLine.IndexOf("Card Number		[") > 0)
                                {
                                    _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 12, 17).Trim();
                                }
                                else _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();
                                _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                                _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                                //---------------------------------
                                string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pickup Count ") + 12, 26); //20200121 change from 24 to 26
                                int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                                string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                                string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count ") + 11, 26);  //20200121 change 24 to 26
                                int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                                string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                                //---------------------------------
                                //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 1:" + counter.ToString());
                                var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 1:" + counter.ToString(), _pickupcount_4, _remaincount_4);
                                csv.AppendLine(newLine);
                                if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                            }
                            /*else if (readCurrentLine.Contains("ATM ID      :") && readCurrentLine.Contains("CARDLESS"))
                            {
                                _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                                _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                                _kartu = "CARDLESS";//readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                                //_reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("Trans SEQ Number") + 18, 4).Trim();
                                _reference = "" + ConvertToInt(readCurrentLine.Substring(readCurrentLine.IndexOf("CARDLESS WITHDRAWAL") + 18 -200, 30).Trim()).ToString();
                                _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                                var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN");
                                csv.AppendLine(newLine);
                                if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                            }*/
                            else if ((readCurrentLine.Contains("ATM ID      :")) && !(readCurrentLine.Contains("ATM ID      :") && readCurrentLine.Contains("CARDLESS")))
                            {
                                _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                                _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                                /* disable 20191115 regarding salah nomor kartu
                                 * if (readCurrentLine.IndexOf("Card Number: ") > 0)
                                {
                                    _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 12, 17).Trim();
                                }
                                else*/
                                _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();

                                _reference = "" + readCurrentLine.Substring(readCurrentLine
                                    .IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                                _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                                //---------------------------------
                                string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pickup Count ") + 12, 26); //20200121 change 24 to 26
                                int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                                string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                                string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count ") + 11, 26);  //20200121 change 24 to 26
                                int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                                string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                                //---------------------------------
                                //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 2:" + counter.ToString());
                                var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 2:" + counter.ToString(), _pickupcount_4, _remaincount_4);
                                csv.AppendLine(newLine);
                                if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                            }
                            else if ((readCurrentLine.Contains("ATM ID     :")) && !(readCurrentLine.Contains("ATM ID      :") && readCurrentLine.Contains("CARDLESS")))
                            {
                                _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID     :") - 29, 29).Replace("+", "").Trim(); //20191031 change from 24 to 29
                                _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID     :") + 12, 8).Trim();

                                /*disable 20191115 regarding salah nomor kartu
                                 * if (readCurrentLine.IndexOf("Card Number: ") > 0)
                                {
                                    _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 12, 17).Trim();
                                }
                                else*/
                                _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();
                                _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.    :") + 12, 6).Trim();
                                _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT     :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                                //---------------------------------
                                string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pickup Count ") + 12, 26); //20191031 change from 24 to 29
                                int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                                string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                                string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count ") + 11, 26);  //20200121 change 24 to 26
                                int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                                string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                                //---------------------------------
                                //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 3:" + counter.ToString());
                                var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 3:" + counter.ToString(), _pickupcount_4, _remaincount_4);
                                csv.AppendLine(newLine);
                                if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                            }
                        }
                    if (readCurrentLine.Contains("SETOR      "))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70,100));
                        int startIndex = readCurrentLine.IndexOf("ATM ID      :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            /*if (readCurrentLine.IndexOf("Card Number: ") > 0)
                            {
                                _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 12, 17).Trim();
                            }
                            else _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();*/
                            _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();
                            if (readCurrentLine.Contains("NO. URUT    :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT    :") + 13, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("NO. URUT  :") + 13, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("SETOR       :") + 13, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Replace("B", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Notes Counted:") + 12, 60);
                            int _pickupcount_2 = _pickupcount_1.IndexOf(":"); int _pickupcount_3 = _pickupcount_1.IndexOf("Total Amount");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1);
                            //---------------------------------
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "SETORAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", "").Replace("O", ""), "ID 2:" + counter.ToString(), "", "", _pickupcount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("DEPOSIT      "))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70,100));
                        int startIndex = readCurrentLine.IndexOf("ATM ID      :");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();

                            /*if (readCurrentLine.IndexOf("Card Number: ") > 0)
                            {
                                _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 13, 16).Trim();
                            }
                            else _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();*/
                            _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();

                            if (readCurrentLine.Contains("SEQ.NO.     :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 13, 6).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("SEQ.NO.     :") + 11, 6).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Replace("B", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Notes Counted:") + 12, 60);
                            int _pickupcount_2 = _pickupcount_1.IndexOf(":"); int _pickupcount_3 = _pickupcount_1.IndexOf("Total Amount");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1);
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "SETORAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 4:" + counter.ToString());
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "SETORAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 4:" + counter.ToString(), "", "", _pickupcount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("WIDTHDRAWAL") || readCurrentLine.Contains("WITHDRAWAL"))
                    {
                        if (readCurrentLine.Contains("ATM ID      :") && readCurrentLine.Contains("CARDLESS"))
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") - 29, 29).Replace("+", "").Replace("/", "-").Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ATM ID      :") + 13, 8).Trim();
                            _kartu = "CARDLESS";//readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number		[") + 14, 16).Trim();
                            //_reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("Trans SEQ Number") + 18, 4).Trim();
                            _reference = "" + ConvertToInt(readCurrentLine.Substring(readCurrentLine.IndexOf("CARDLESS WITHDRAWAL") + 18 - 200, 30).Trim()).ToString();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("AMOUNT      :") + 13, 17).Replace("B", "").Replace("A", "").Replace("L", "").Replace("N", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Pickup Count ") + 12, 26); //20200121 change 24 to 26
                            int _pickupcount_2 = _pickupcount_1.IndexOf("["); int _pickupcount_3 = _pickupcount_1.IndexOf("]");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1).Replace(",", ",");
                            string _remaincount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Remain Count ") + 11, 26);  //20200121 change 24 to 26
                            int _remaincount_2 = _remaincount_1.IndexOf("["); int _remaincount_3 = _remaincount_1.IndexOf("]");
                            string _remaincount_4 = _remaincount_1.Substring(_remaincount_2 + 1, _remaincount_3 - _remaincount_2 - 1).Replace(",", ",");
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 5:" + counter.ToString());
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "PENARIKAN", _amount.Replace("Rp.", "").Replace(".00", "").Replace(",", ""), "EN 5:" + counter.ToString(), _pickupcount_4, _remaincount_4);
                            csv.AppendLine(newLine);
                            if (chkJoin.Checked) globalcsv.AppendLine(newLine);
                        }
                    }
                    if (readCurrentLine.Contains("Emergency Receipt"))
                    {
                        //MessageBox.Show(readCurrentLine.Substring(70,100));
                        int startIndex = readCurrentLine.IndexOf("Emergency Receipt");
                        if (startIndex > 0)
                        {
                            _datetime = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Tanggal :") + 9, 16).Replace("+", "").Replace("/", "-").Trim(); //20191031 change from 24 to 29
                            _atmid = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("ID CRM  :") + 9, 9).Trim();
                            /*if (readCurrentLine.IndexOf("Card Number: ") > 0)
                            {
                                _kartu = "'" + readCurrentLine.Substring(readCurrentLine.IndexOf("Card Number: ") + 12, 17).Trim();
                            }
                            else _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();*/
                            _kartu = "'" + readCurrentLine.Substring(0, 17).Trim();
                            if (readCurrentLine.Contains("No Resi :")) _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("No Resi :") + 9, 5).Trim();
                            else _reference = "" + readCurrentLine.Substring(readCurrentLine.IndexOf("No Resi :") + 13, 5).Trim();
                            _amount = readCurrentLine.Substring(readCurrentLine.IndexOf("Jumlah  :") + 9, 17).Replace("S", "").Replace("A", "").Replace("L", "").Replace("D", "").Replace("-", "").Trim();
                            //---------------------------------
                            string _pickupcount_1 = readCurrentLine.Substring(readCurrentLine.IndexOf("Notes Counted:") + 12, 60);
                            int _pickupcount_2 = _pickupcount_1.IndexOf(":"); int _pickupcount_3 = _pickupcount_1.IndexOf("Total Amount");
                            string _pickupcount_4 = _pickupcount_1.Substring(_pickupcount_2 + 1, _pickupcount_3 - _pickupcount_2 - 1);
                            //---------------------------------
                            //var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "EMERGENCY DEPOSIT", _amount.Replace("Rp.", "").Replace(",", "").Replace(".", ""), "ID 3:" + counter.ToString());
                            var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", _datetime.Substring(0, 11).Trim(), _datetime.Substring(12, _datetime.Length - 12).Trim().Replace(":", ""), _atmid, _kartu, _reference, "EMERGENCY DEPOSIT", _amount.Replace("Rp.", "").Replace(",", "").Replace(".", ""), "ID 3:" + counter.ToString(), "", "", _pickupcount_4);
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
            try
            {
                //if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt", ".csv"), csv.ToString());
                if (!chkJoin.Checked) File.WriteAllText(_path.Replace(".txt_", ".csv"), csv.ToString());
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
            String _newpath = "";
            if (_path.Contains(".dat"))
            {
                _newpath = _path.Replace(".dat", ".txt_");
            }
            else if (_path.Contains(".txt"))
            {
                _newpath = _path.Replace(".txt", ".txt_");
            }
            // This text is added only once to the file.
            if (!File.Exists(_newpath))
            {
                File.WriteAllText(_newpath, _content);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "Contact didik.haryadi@kebhana.co.id  " + Environment.NewLine + "       If there's a change in EJ format!";
            string title = "Help!";
            MessageBox.Show(message, title);
        }

        public static int ConvertToInt(String input)
        {
            // Matches the first numebr with or without leading minus.
            Match match = Regex.Match(input, "-?[0-9]+");

            if (match.Success)
            {
                // No need to TryParse here, the match has to be at least
                // a 1-digit number.
                return int.Parse(match.Value);
            }

            return 0; // Or any other default value.
        }

    }
}
