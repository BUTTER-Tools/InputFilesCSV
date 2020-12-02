using System;
using PluginContracts;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using OutputHelperLib;
using System.Linq;

namespace InputFileCSV
{
    public class InputFilesCSV : InputPlugin
    {

        public string[] InputType { get; } = { "CSV File" };
        public string OutputType { get; } = "String";
        public StreamReader InputStream { get; set; }
        //public object Output { get; set; }
        public bool KeepStreamOpen { get; } = true;
        public string IncomingTextLocation { get; set; }
        public string SelectedEncoding { get; set; } = "utf-8";
        private string Delimiter = ",";
        private string Quote = "\"";
        public int[] CSV_ID_Indices { get; set; } = { };
        public int[] CSV_Text_Indices { get; set; } = { };
        public string[] CSVHeaders { get; set; } = { };
        public bool TextColumnsSeparate { get; set; } = false;
        ulong RowCounter { get; set; } = 0;
        public bool InheritHeader { get; } = false;
        public Dictionary<int, string> OutputHeaderData { get; set;  } = new Dictionary<int, string>(){
                                                                                            {0, "Text"}
                                                                                        };
        public int TextCount { get; set; }
        public string[] header { get; set; }



        #region Plugin Details and Info

        public string PluginName { get; } = "Read Text from CSV";
        public string PluginType { get; } = "Load File(s)";
        public string PluginVersion { get; } = "1.0.2";
        public string PluginAuthor { get; } = "Ryan L. Boyd (ryan@ryanboyd.io)";
        public string PluginDescription { get; } = "This plugin will read text(s) from a CSV spreadsheet file. Use the Plugin Settings to select the CSV file that contains your texts. This plugin should always be at the top level of your Analysis Pipeline. For example:" + Environment.NewLine + Environment.NewLine + Environment.NewLine +
            "\tRead Text from CSV" + Environment.NewLine +
            "\t |" + Environment.NewLine +
            "\t |-- Tokenize Texts" + Environment.NewLine +
            "\t |" + Environment.NewLine +
            "\t |-- etc.";
        public bool TopLevel { get; } = true;
        public string PluginTutorial { get; } = "https://youtu.be/AkrwGcGEfvo";


        public Icon GetPluginIcon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }

        #endregion


        #region Settings and ChangeSettings() Method
        private bool ScanSubfolders { get; } = false;
               
        public void ChangeSettings()
        {
            using (var form = new SettingsForm_InputFileCSV(IncomingTextLocation, SelectedEncoding, Delimiter, Quote,
                                                            CSV_ID_Indices, CSV_Text_Indices, CSVHeaders, TextColumnsSeparate))
            {


                form.Icon = Properties.Resources.icon;
                form.Text = PluginName;


                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    SelectedEncoding = form.SelectedEncoding;
                    IncomingTextLocation = form.CSVFileLocation;
                    Delimiter = form.Delimiter;
                    Quote = form.Quote;
                    CSVHeaders = form.Headers;
                    CSV_ID_Indices = form.CSV_ID_Indices;
                    CSV_Text_Indices = form.CSV_Text_Indices;
                    TextColumnsSeparate = form.TextColumnsSeparate;
                }
            }

        }
        #endregion


        public Payload RunPlugin(Payload Incoming)
        {
            RowCounter++;

            Payload pData = new Payload();
            pData.FileID = Incoming.FileID;
            pData.SegmentID = Incoming.SegmentID;

            string[] DataRow;
            DataRow = (Incoming.ObjectList[0] as IList<string>).ToArray<string>();

            if (CSV_ID_Indices.Length > 0)
            {
                string[] FileID = new string[CSV_ID_Indices.Length];
                for (int counter = 0; counter < CSV_ID_Indices.Length; counter++) FileID[counter] = DataRow[CSV_ID_Indices[counter]];
                pData.FileID = (string.Join(";", FileID));
            }
            else
            {
                pData.FileID = Incoming.TextNumber.ToString();
            }


            try
            {

                if (TextColumnsSeparate)
                {
                    //old version
                    //for (int counter = 0; counter < CSV_Text_Indices.Length; counter++) FileContents.Add(((IList<string>)Incoming)[CSV_Text_Indices[counter]]);
                    for (int counter = 0; counter < CSV_Text_Indices.Length; counter++)
                    {
                        pData.StringList.Add(DataRow[CSV_Text_Indices[counter]].Trim());
                        pData.SegmentNumber.Add(1);
                        pData.SegmentID.Add(header[CSV_Text_Indices[counter]]);
                    }
                }
                else
                {
                    StringBuilder Texts = new StringBuilder();
                    StringBuilder SegID = new StringBuilder();
                    for (int counter = 0; counter < CSV_Text_Indices.Length; counter++)
                    {
                        Texts.AppendLine(DataRow[CSV_Text_Indices[counter]]);
                        SegID.Append(header[CSV_Text_Indices[counter]] + ";");
                    }
                    pData.StringList.Add(Texts.ToString().Trim());
                    pData.SegmentNumber.Add(1);
                    pData.SegmentID.Add(SegID.ToString());

            }

                return (pData);

            }
            catch
            {
                return (new Payload());
            }
        }





        public IEnumerable TextEnumeration()
        {
            RowCounter = 0;

            try
            {

                    var data = CsvParser.ParseHeadAndTail(InputStream, Delimiter[0], Quote[0]);

                    header = data.Item1.ToArray();
                    var lines = data.Item2;
                
                    return lines;

            }
            catch
            {
                MessageBox.Show("There was an error while trying to read your spreadsheet file. It is possible that your spreadsheet is not correctly formatted, or that your selections for delimiters and quotes are not the same as what is used in your spreadsheet.", "Error reading spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                IEnumerable<IList<string[]>> nada = Enumerable.Empty<IList<string[]>>();
                return (nada);
            }


        }


        //for input streams, we use the Initialize() method to tally up the number of items to be analyzed
        public void Initialize()
        {

            //InputStream = new StreamReader(File.OpenRead(IncomingTextLocation), encoding: Encoding.GetEncoding(SelectedEncoding));
            TextCount = 0;

            using (var stream = File.OpenRead(IncomingTextLocation))
            using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
            {
                var data = CsvParser.ParseHeadAndTail(reader, Delimiter[0], Quote[0]);

                header = data.Item1.ToArray();
                var lines = data.Item2;

                foreach (var line in lines)
                {
                    try
                    {
                        TextCount++;
                    }
                    catch
                    {
                        
                    }


                }

            }
        }





        public bool InspectSettings()
        {
            if (string.IsNullOrEmpty(IncomingTextLocation))
            {
                return false;
            }
            else if (!File.Exists(IncomingTextLocation))
            {
                MessageBox.Show("Your selected input file does not appear to exist anymore. Has it been deleted/moved?", "Cannot Find Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
            
        }



        public Payload FinishUp(Payload Input)
        {
            return (Input);
        }



        #region Import/Export Settings
        public void ImportSettings(Dictionary<string, string> SettingsDict)
        {
            SelectedEncoding = SettingsDict["SelectedEncoding"];
            IncomingTextLocation = SettingsDict["IncomingTextLocation"];
            Delimiter = SettingsDict["Delimiter"];
            Quote = SettingsDict["Quote"];
            TextColumnsSeparate = Boolean.Parse(SettingsDict["TextColumnsSeparate"]);

            int CSVHeaderLength = int.Parse(SettingsDict["CSVHeaderLength"]);
            int CSV_ID_IndicesLength = int.Parse(SettingsDict["CSV_ID_IndicesLength"]);
            int CSV_Text_IndicesLength = int.Parse(SettingsDict["CSV_Text_IndicesLength"]);

            CSVHeaders = new string[CSVHeaderLength];
            CSV_ID_Indices = new int[CSV_ID_IndicesLength];
            CSV_Text_Indices = new int[CSV_Text_IndicesLength];

            for (int i = 0; i < CSVHeaderLength; i++)
            {
                CSVHeaders[i] = SettingsDict["CSVHeader" + i.ToString()];
            }

            for (int i = 0; i < CSV_ID_IndicesLength; i++)
            {
                CSV_ID_Indices[i] = int.Parse(SettingsDict["CSV_ID_Indices" + i.ToString()]);
            }

            for (int i = 0; i < CSV_Text_IndicesLength; i++)
            {
                CSV_Text_Indices[i] = int.Parse(SettingsDict["CSV_Text_Indices" + i.ToString()]);
            }

        }

        public Dictionary<string, string> ExportSettings(bool suppressWarnings)
        {
            Dictionary<string, string> SettingsDict = new Dictionary<string, string>();
            SettingsDict.Add("SelectedEncoding", SelectedEncoding);
            SettingsDict.Add("IncomingTextLocation", IncomingTextLocation);
            SettingsDict.Add("Delimiter", Delimiter);
            SettingsDict.Add("Quote", Quote);
            SettingsDict.Add("TextColumnsSeparate", TextColumnsSeparate.ToString());

            int CSVHeadersLength = 0;
            if (CSVHeaders != null) CSVHeadersLength = CSVHeaders.Length;
            int CSV_ID_IndicesLength = 0;
            if (CSV_ID_Indices != null) CSV_ID_IndicesLength = CSV_ID_Indices.Length;
            int CSV_Text_IndicesLength = 0;
            if (CSV_Text_Indices != null) CSV_Text_IndicesLength = CSV_Text_Indices.Length;

            SettingsDict.Add("CSVHeaderLength", CSVHeadersLength.ToString());
            for (int i = 0; i < CSVHeadersLength; i++)
            {
                SettingsDict.Add("CSVHeader" + i.ToString(), CSVHeaders[i]);
            }

            SettingsDict.Add("CSV_ID_IndicesLength", CSV_ID_IndicesLength.ToString());
            for (int i = 0; i < CSV_ID_IndicesLength; i++)
            {
                SettingsDict.Add("CSV_ID_Indices" + i.ToString(), CSV_ID_Indices[i].ToString());
            }

            SettingsDict.Add("CSV_Text_IndicesLength", CSV_Text_IndicesLength.ToString());
            for (int i = 0; i < CSV_Text_IndicesLength; i++)
            {
                SettingsDict.Add("CSV_Text_Indices" + i.ToString(), CSV_Text_Indices[i].ToString());
            }
            return (SettingsDict);
        }
        #endregion

    }
}
