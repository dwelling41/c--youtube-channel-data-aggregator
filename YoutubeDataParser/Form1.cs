using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;

namespace YoutubeDataParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            
            openFileDialog1.Filter = "xlsx (*.xlsx)|*.xlsx|xls (*.xls)|*.xls|xlsm (*.xlsm)|*.xlsm";
            openFileDialog1.ShowDialog();
            this.txtPathToExcel.Text = openFileDialog1.FileName;
        }

        private void EnableDisableForm(bool enabled)
        {
            btnRun.Enabled = enabled;
            txtAPIKey.Enabled = enabled;
            txtChannelUrl.Enabled = enabled;
            txtPathToExcel.Enabled = enabled;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                EnableDisableForm(false);

                // Make sure the API key is set
                if (string.IsNullOrEmpty(this.txtAPIKey.Text))
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Please enter your API key.");
                    return;
                }

                // Make sure the excel document is set
                if (string.IsNullOrEmpty(this.txtPathToExcel.Text))
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Please enter a path to the Excel document");
                    return;
                }

                // Parse out the channel url
                if (string.IsNullOrEmpty(this.txtChannelUrl.Text))
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Please enter a user's channel to load data for");
                    return;
                }

                var indexOfUserQueryString = this.txtChannelUrl.Text.Trim().LastIndexOf("/user/");
                if (indexOfUserQueryString < 0)
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Please enter a valid user's channel");
                    return;
                }

                var username = this.txtChannelUrl.Text.Trim().Substring(indexOfUserQueryString + "/user/".Length);
                if (indexOfUserQueryString < 0)
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Please enter a valid user's channel");
                    return;
                }


                // Get the youtube channel data
                var dataGatherer = new ChannelStatisticsGatherer(txtAPIKey.Text);
                string errorMessage = "";
                var allData = dataGatherer.GetChannelStatistics(username, out errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    EnableDisableForm(true);
                    MessageBox.Show("Error: " + errorMessage);
                    return;
                }

                if (allData == null || allData.Count <= 0)
                {
                    EnableDisableForm(true);
                    MessageBox.Show("No data returned.");
                    return;
                }

                // Load the excel workbook
                using (var excelDoc = new ExcelPackage(new System.IO.FileInfo(this.txtPathToExcel.Text)))
                {
                    var workbook = excelDoc.Workbook.Worksheets.First();

                    // Find the first non-filled row after row 8. Cap to a reasonable number of rows
                    int currentRowId = -1;
                    for (int i = 9; i < 100000; i++)
                    {
                        if (workbook.Cells[i, 1].Value == null)
                        {
                            currentRowId = i;
                            break;
                        }
                    }

                    if (currentRowId < 0)
                    {
                        EnableDisableForm(true);
                        MessageBox.Show("Couldn't find an empty row in the Excel document.");
                        return;
                    }

                    // Write the data
                    foreach (var curChannel in allData)
                    {
                        // Print text fields
                        workbook.Cells[currentRowId, 1].Value = txtChannelUrl.Text.Trim();
                        workbook.Cells[currentRowId, 2].Value = curChannel.ChannelName;
                        workbook.Cells[currentRowId, 3].Value = curChannel.ChannelLanguage;

                        // Format all cells as numbers after language
                        workbook.Cells[currentRowId, 4, currentRowId, 10].Style.Numberformat.Format = "#";
                        workbook.Cells[currentRowId, 4].Value = curChannel.Subscribers;
                        workbook.Cells[currentRowId, 5].Value = curChannel.LastMonthVideoData.Count;
                        workbook.Cells[currentRowId, 6].Value = curChannel.LastMonthVideoData.Sum(x => (long)x.Views.GetValueOrDefault());
                        workbook.Cells[currentRowId, 7].Value = curChannel.LastMonthVideoData.Sum(x => (long)x.Likes.GetValueOrDefault());
                        workbook.Cells[currentRowId, 8].Value = curChannel.LastMonthVideoData.Sum(x => (long)x.Dislikes.GetValueOrDefault());
                        workbook.Cells[currentRowId, 9].Value = curChannel.LastMonthVideoData.Sum(x => (long)x.Comments.GetValueOrDefault());
                        workbook.Cells[currentRowId, 10].Value = curChannel.LastMonthVideoData.Sum(x => x.Length.GetValueOrDefault().TotalMinutes);

                        currentRowId++;
                    }

                    excelDoc.Save();
                }

                btnRun.Enabled = true;
                txtAPIKey.Enabled = true;
                txtChannelUrl.Enabled = true;
                txtPathToExcel.Enabled = true;
                MessageBox.Show("Complete!");
                EnableDisableForm(true);
            }
            catch(Exception ex)
            {
                EnableDisableForm(true);
                MessageBox.Show("Unhandled Exception: " + ex.Message);
            }
            
        }
    }
}
