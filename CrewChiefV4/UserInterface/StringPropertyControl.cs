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
    public partial class StringPropertyControl : UserControl
    {
        public String propertyId;
        public String defaultValue;
        public String originalValue;
        public String label;
        internal PropertyFilter filter = null;
        public StringPropertyControl(String propertyId, String label, String currentValue, String defaultValue, String helpText, String filterText)
        {
            InitializeComponent();

            this.SuspendLayout();
            this.label = label;
            this.propertyId = propertyId;
            this.label1.Text = label;
            this.originalValue = currentValue;
            this.textBox1.Text = currentValue;
            this.defaultValue = defaultValue;
            this.toolTip1.SetToolTip(this.textBox1, helpText);
            this.toolTip1.SetToolTip(this.label1, helpText);
            this.ResumeLayout(false);

            this.filter = new PropertyFilter(filterText, propertyId, this.label);
        }

        public String getValue()
        {
            return this.textBox1.Text;
        }

        public void button1_Click(object sender, EventArgs e)
        {
            if (originalValue != defaultValue)
            {
                PropertiesForm.hasChanges = true;
            }
            this.textBox1.Text = defaultValue;
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (this.textBox1.Text != originalValue)
            {
                PropertiesForm.hasChanges = true;
            }
        }
    }
}
