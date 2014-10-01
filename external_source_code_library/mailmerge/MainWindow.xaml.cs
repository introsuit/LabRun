using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Office.Interop;
using System.Globalization;
using Microsoft.Win32;
using System.IO;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        List<Participant> adat = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        Microsoft.Office.Interop.Word.Application wrdApp;
        Microsoft.Office.Interop.Word._Document wrdDoc;
        Object oMissing = System.Reflection.Missing.Value;
        Object oFalse = false;



        private void InsertLines(int LineNum)
        {
            int iCount;

            // Insert "LineNum" blank lines.	
            for (iCount = 1; iCount <= LineNum; iCount++)
            {
                wrdApp.Selection.TypeParagraph();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options
            openFileDialog1.Filter = "ZTree Pay Files (*.pay)|*.pay|All Files|*.*";
            

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                readPayFile(openFileDialog1.FileName);
                Microsoft.Office.Interop.Word.Selection wrdSelection;
                Microsoft.Office.Interop.Word.MailMerge wrdMailMerge;
                Microsoft.Office.Interop.Word.MailMergeFields wrdMergeFields;
                Microsoft.Office.Interop.Word.Table wrdTable;
                string StrToAdd;

                // Create an instance of Word  and make it visible.
                wrdApp = new Microsoft.Office.Interop.Word.Application();
                wrdApp.Visible = true;

                // Add a new document.
                wrdDoc = wrdApp.Documents.Add(ref oMissing, ref oMissing,
                    ref oMissing, ref oMissing);
                wrdDoc.Select();

                wrdSelection = wrdApp.Selection;



                foreach (Participant partip in this.adat)
                {
                    // Create a MailMerge Data file.
                    //  CreateMailMergeDataFile();

                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphRight;
                    wrdSelection.Font.Size = 26;
                    wrdSelection.Font.Name = "Arial";
                    wrdSelection.TypeText("Receipt \r\n Seat number: " + partip.boothno);
                    wrdSelection.Font.Size = 10;
                    wrdSelection.Font.Name = "Times New Roman";

                    InsertLines(1);

                    // Create a string and insert it into the document.
                    wrdSelection.ParagraphFormat.Alignment =
                       Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphLeft;
                    // set color waaaaa                 wrdSelection.Font.TextColor.RGB = 000080;

                    StrToAdd = "\r\n COGNITION AND BEHAVIOR LAB";
                    // wrdSelection.ParagraphFormat.Alignment  = 
                    //   Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;



                    try
                    {
                       
                        string location = Application.ResourceAssembly.Location;
                        //MessageBox.Show(location);
                        location = location.Replace(@"\merge.exe", "");
                        //MessageBox.Show(location);
                        location = location + @"\1.png";
                        //MessageBox.Show(location);
                        wrdSelection.InlineShapes.AddPicture(location);
                        

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Image file (1.png) not found!");
                    }
                    wrdSelection.TypeText(StrToAdd);



                    wrdSelection.TypeText("\r\nName: " + partip.name);
                    wrdSelection.TypeText("\r\nCPR number: " + partip.cpr);
                    wrdSelection.TypeText("\r\nYour earnings in today’s experiment: " + partip.profit + " kr. \r\n");

                    // Justify the rest of the document.
                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;

                    // Create a string and insert it into the document.
                    wrdSelection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;
                    StrToAdd = "Aarhus University will automatically transfer the amount you earn into your NemKonto (for this we need your CPR number). This is simply your existing bank account, into which all payments from the public sector flow (e.g. tax refunds or SU student grants). Alexander Koch and his team will start registering the payments with the administration of Aarhus University this week. Then the administrative process might take between 2-6 weeks. You can contact Alexander Koch by email (akoch@econ.au.dk) if you want information on the payment process.";
                    wrdSelection.TypeText(StrToAdd);
                    wrdSelection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;
                    InsertLines(1);
                    StrToAdd = "According to Danish law, Aarhus University reports payments to the tax authorities. Please note that, depending on your personal income tax rate, taxes will be deducted from the amount of money you earn in this study. That is, the amount you will receive might be lower than the pre-tax earnings stated above.";
                    wrdSelection.TypeText(StrToAdd);
                    InsertLines(1);

                    // Right justify the line and insert a date field
                    // with the current date.
                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphRight;

                    Object objDate = "dddd, MMMM dd, yyyy";
                    wrdSelection.InsertDateTime(ref objDate, ref oFalse, ref oMissing,
                        ref oMissing, ref oMissing);

                    wrdSelection.InlineShapes.AddHorizontalLineStandard();
                    wrdSelection.InsertBreak(Microsoft.Office.Interop.Word.WdBreakType.wdPageBreak);


                    // Close the original form document.
                    //wrdDoc.Saved = true;
                    /*wrdDoc.Close(ref oFalse,ref oMissing,ref oMissing);


                    // Release References.
                    wrdSelection = null;
                    wrdMailMerge = null;
                    wrdMergeFields = null;
                    wrdDoc = null;
                    wrdApp = null;

                    // Clean up temp file.
                    System.IO.File.Delete("C:\\DataDoc.doc");*/
                }


            }
        }

        private void readPayFile(string path)
        {
            string line = "";
            int subject = -1;
            string name = "";
            string cpr = "";
            decimal pay;
            this.adat = new List<Participant>();

            using (System.IO.StreamReader file = new System.IO.StreamReader(path))
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] temp = line.Split('	');
                    try
                    {
                        subject = Int32.Parse(temp[0]);
                        string[] temp2 = temp[3].Split(',');
                        name = temp2[0];
                        cpr = temp2[1];
                        pay = decimal.Parse(temp[4], CultureInfo.InvariantCulture);
                        Participant particip = new Participant(name, cpr, subject, pay);
                        adat.Add(particip);


                    }
                    catch (Exception ex)
                    {
                    }



                }
                /* foreach (Participant part in adat)
                 {
                     MessageBox.Show(part.boothno + "");
                     MessageBox.Show(part.name);
                     MessageBox.Show(part.cpr);
                     MessageBox.Show(part.profit + "");
                 }*/
            }
        }
    }
}
