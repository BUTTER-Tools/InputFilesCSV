using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Collections.Generic;





namespace InputFileCSV
{
    internal partial class SettingsForm_InputFileCSV : Form
    {


        #region Get and Set Options

        public string CSVFileLocation { get; set; }
        public string SelectedEncoding { get; set; }
        public string Delimiter { get; set; }
        public string Quote { get; set; }
        public int[] CSV_ID_Indices { get; set; }
        public int[] CSV_Text_Indices { get; set; }
        public string[] Headers;
        public bool TextColumnsSeparate;

        #endregion



        public SettingsForm_InputFileCSV(string CSVFileLocation, string SelectedEncoding, string Delimiter, string Quote,
                                         int[] CSV_ID_Indices, int[] CSV_Text_Indices, string[] CSVHeaders, bool TextColumnsSeparate)
        {
            InitializeComponent();

            foreach (var encoding in Encoding.GetEncodings())
            {
                EncodingDropdown.Items.Add(encoding.Name);
            }

            try
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(SelectedEncoding);
            }
            catch
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(Encoding.Default.BodyName);
            }

            CSVDelimiterTextbox.Text = Delimiter;
            CSVQuoteTextbox.Text = Quote;
            SelectedFileTextbox.Text = CSVFileLocation;
            Headers = CSVHeaders;
            this.SelectedEncoding = SelectedEncoding;

            ID_CheckedListBox.Items.AddRange(Headers);
            Text_CheckedListBox.Items.AddRange(Headers);

            foreach (int index in CSV_ID_Indices) ID_CheckedListBox.SetItemChecked(index, true);
            foreach (int index in CSV_Text_Indices) Text_CheckedListBox.SetItemChecked(index, true);

            ColumnsAsSeparateTextsCheckbox.Checked = TextColumnsSeparate;

        }












        private void SetFolderButton_Click(object sender, System.EventArgs e)
        {

            ID_CheckedListBox.Items.Clear();
            Text_CheckedListBox.Items.Clear();
            SelectedFileTextbox.Text = "";
            Headers = new string[0];

            CSV_ID_Indices = new int[0];
            CSV_Text_Indices = new int[0];

            if (CSVDelimiterTextbox.TextLength < 1 || CSVQuoteTextbox.TextLength < 1)
            {
                MessageBox.Show("You must enter characters for your delimiter and quotes, respectively. MEH does not know how to read a delimited spreadsheet without this information.", "I need details for your spreadsheet!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }




            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;
                dialog.Title = "Please choose the CSV file that you would like to read";
                dialog.FileName = "CSV Spreadsheet.csv";
                dialog.Filter = "Comma-Separated Values (CSV) File (*.csv)|*.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedFileTextbox.Text = dialog.FileName;


                    try
                    {
                        using (var stream = File.OpenRead(dialog.FileName))
                        using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                        {
                            var data = CsvParser.ParseHeadAndTail(reader, CSVDelimiterTextbox.Text[0], CSVQuoteTextbox.Text[0]);

                            var header = data.Item1;
                            var lines = data.Item2;

                            string[] HeadersFromFile = header.ToArray<string>();

                            ID_CheckedListBox.Items.AddRange(HeadersFromFile);
                            Text_CheckedListBox.Items.AddRange(HeadersFromFile);

                            Headers = HeadersFromFile;

                        }

                    }
                    catch
                    {
                        MessageBox.Show("There was an error while trying to read your spreadsheet file. It is possible that your spreadsheet is not correctly formatted, or that your selections for delimiters and quotes are not the same as what is used in your spreadsheet.", "Error reading spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                }
            }
        }
















        private void OKButton_Click(object sender, System.EventArgs e)
        {
            this.SelectedEncoding = EncodingDropdown.SelectedItem.ToString();
            this.CSVFileLocation = SelectedFileTextbox.Text;
            this.TextColumnsSeparate = ColumnsAsSeparateTextsCheckbox.Checked;
            
            List<int> CheckedListboxResults = new List<int>();

            foreach (object CheckedItem in ID_CheckedListBox.CheckedItems) CheckedListboxResults.Add(ID_CheckedListBox.Items.IndexOf(CheckedItem));
            this.CSV_ID_Indices = CheckedListboxResults.ToArray();

            CheckedListboxResults = new List<int>();
            foreach (object CheckedItem in Text_CheckedListBox.CheckedItems) CheckedListboxResults.Add(Text_CheckedListBox.Items.IndexOf(CheckedItem));
            this.CSV_Text_Indices = CheckedListboxResults.ToArray();


            if (CSVQuoteTextbox.Text.Length > 0)
            {
                this.Quote = CSVQuoteTextbox.Text;
            }
            else
            {
                this.Quote = "\"";
            }
            if (CSVDelimiterTextbox.Text.Length > 0)
            {
                this.Delimiter = CSVDelimiterTextbox.Text;
            }
            else
            {
                this.Delimiter = ",";
            }
            

            this.DialogResult = DialogResult.OK;

        }
    }
}
