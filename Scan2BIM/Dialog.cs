using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;

namespace Scan2BIM
{
    public partial class Dialog : System.Windows.Forms.Form
    {
        Document Doc;


        public Dialog(Document doc)
        {
            InitializeComponent();
            Doc = doc;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select File";
            openFileDialog.InitialDirectory = @"F:\";
            openFileDialog.Filter = "All files (*.*)|*.*|Text File (*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
            {
                textBox1.Text = openFileDialog.FileName;

                string path = openFileDialog.FileName;


                //Read all the elements' coordinates from the file
                string texts = File.ReadAllText(path);

                textBox2.Text=texts;

            }
            else
            {
                textBox1.Text = "";
                textBox2.Text = "";
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                S2BCommand handler = new S2BCommand();
                handler.FilePath = textBox1.Text;
                ExternalEvent externalEvent = ExternalEvent.Create(handler);
                externalEvent.Raise();
                


                //Utility.ExecuteBIMGeneration(this.Doc, textBox1.Text);
                this.Close();
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void Dialog_Load(object sender, EventArgs e)
        {

        }
    }
}
