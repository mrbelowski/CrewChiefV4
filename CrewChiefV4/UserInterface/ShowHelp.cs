using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChiefV4.UserInterface
{
    public partial class ShowHelp : Form
    {
        public ShowHelp(System.Windows.Forms.Form parent)
        {
            InitializeComponent();
            String path = Configuration.getDefaultFileLocation(CrewChief.Debugging ? "..\\help.txt" : "help.txt");
            textBox1.Text = File.ReadAllText(path);
            textBox1.Select(0, 0);

            this.KeyPreview = true;
            this.KeyDown += ShowHelp_KeyDown;
        }

        private void ShowHelp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                this.textBox1.SelectAll();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
