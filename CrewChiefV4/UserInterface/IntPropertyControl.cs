using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV4
{
    public partial class IntPropertyControl : UserControl
    {
        public String propertyId;
        public int originalValue;
        public int defaultValue;
        public IntPropertyControl(String propertyId, String label, int value, int defaultValue, String helpText)
        {
            InitializeComponent();
            this.propertyId = propertyId;
            this.label1.Text = label;
            this.originalValue = value;
            this.textBox1.Text = value.ToString();
            this.defaultValue = defaultValue;
            this.toolTip1.SetToolTip(this.textBox1, helpText);
            this.toolTip1.SetToolTip(this.label1, helpText);
        }

        public int getValue()
        {
            int newVal;
            if (int.TryParse(this.textBox1.Text, out newVal))
            {
                originalValue = newVal;
                return newVal;
            }
            else
            {
                return originalValue;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (defaultValue != originalValue)
            {
                PropertiesForm.hasChanges = true;
            }
            this.textBox1.Text = defaultValue.ToString();
            this.originalValue = defaultValue;
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (this.textBox1.Text != originalValue.ToString())
            {
                PropertiesForm.hasChanges = true;
            }
        }
    }
}
