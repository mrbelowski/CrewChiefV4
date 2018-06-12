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
    public partial class BooleanPropertyControl : UserControl
    {
        public String propertyId;
        public String label;
        public Boolean defaultValue;
        public Boolean originalValue;
        internal PropertyFilter filter = null;
        public BooleanPropertyControl(String propertyId, String label, Boolean value, Boolean defaultValue, String helpText, String filterText)
        {
            InitializeComponent();

            this.label = label;
            this.propertyId = propertyId;
            this.originalValue = value;
            this.checkBox1.Text = label;
            this.checkBox1.Checked = value;
            this.defaultValue = defaultValue;
            this.toolTip1.SetToolTip(this.checkBox1, helpText);

            this.filter = new PropertyFilter(filterText, propertyId, this.label);
        }
        public Boolean getValue()
        {
            return this.checkBox1.Checked;
        }

        public void button1_Click(object sender, EventArgs e)
        {
            this.checkBox1.Checked = defaultValue;
            if (this.originalValue != this.checkBox1.Checked)
            {
                PropertiesForm.hasChanges = true;
            }
        }

        private void checkedChanged(object sender, EventArgs e)
        {
            if (this.originalValue != this.checkBox1.Checked)
            {
                PropertiesForm.hasChanges = true;
            }
        }
    }
}
