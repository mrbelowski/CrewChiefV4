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
    public class ListPropertyValues
    {
        public class ListBoxItem
        {
            public String label;
            public String invariantValue;
            public int index;
            public ListBoxItem(String label, String invariantValue, int index)
            {
                this.label = label;
                this.invariantValue = invariantValue;
                this.index = index;
            }
        }

        public static Dictionary<String, List<ListBoxItem>> listBoxData = new Dictionary<string, List<ListBoxItem>>();

        static ListPropertyValues()
        {
            ListBoxItem[] interruptItems = new ListBoxItem[]{
                new ListBoxItem(Configuration.getUIString("interrupts_none"), "NONE", 0),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_only"), CrewChiefV4.Audio.SoundType.SPOTTER.ToString(), 1),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_and_critical"), CrewChiefV4.Audio.SoundType.CRITICAL_MESSAGE.ToString(), 2),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_critical_and_important"), CrewChiefV4.Audio.SoundType.IMPORTANT_MESSAGE.ToString(), 3)
            };
            listBoxData.Add("LISTBOX_interrupt_setting", interruptItems.ToList());
        }

        public static List<String> getListBoxLabels(String id)
        {
            List<String> entries = new List<String>();
            if (ListPropertyValues.listBoxData.ContainsKey(id))
            {
                List<ListBoxItem> items = ListPropertyValues.listBoxData[id];
                foreach (ListBoxItem item in items)
                {
                    entries.Add(item.label);
                }
            }
            return entries;
        }

        public static String getInvariantValueForLabel(String id, String selectedLabel)
        {
            if (ListPropertyValues.listBoxData.ContainsKey(id))
            {
                List<ListBoxItem> items = listBoxData[id];
                foreach (ListBoxItem item in items)
                {
                    if (item.label == selectedLabel)
                    {
                        return item.invariantValue;
                    }
                }
            }
            return null;
        }

        public static String getLabelForInvariantItem(String id, String selectedInvariantValue)
        {
            if (ListPropertyValues.listBoxData.ContainsKey(id))
            {
                List<ListBoxItem> items = listBoxData[id];
                foreach (ListBoxItem item in items)
                {
                    if (item.invariantValue == selectedInvariantValue)
                    {
                        return item.label;
                    }
                }
            }
            return null;
        }
    }
    public partial class ListPropertyControl : UserControl
    {
        public String propertyId;
        public String defaultValue;
        public String originalValue;
        public List<String> availableValues;
        public String label;
        internal PropertyFilter filter = null;

        public ListPropertyControl(String propertyId, String label, String currentValue, 
            String defaultValue, String helpText, String filterText, String categoryText)
        {
            InitializeComponent();

            this.label = label;
            this.propertyId = propertyId;
            this.label1.Text = label;
            this.availableValues = ListPropertyValues.getListBoxLabels(propertyId);
            this.comboBox1.BeginUpdate();
            foreach (String value in availableValues)
            {
                this.comboBox1.Items.Add(value);
            }
            this.comboBox1.EndUpdate();

            this.originalValue = ListPropertyValues.getLabelForInvariantItem(propertyId, currentValue);
            this.comboBox1.Text = this.originalValue;
            this.comboBox1.SelectedIndex = availableValues.IndexOf(this.originalValue);

            this.defaultValue = ListPropertyValues.getLabelForInvariantItem(propertyId, defaultValue);
            this.toolTip1.SetToolTip(this.comboBox1, helpText);
            this.toolTip1.SetToolTip(this.label1, helpText);

            this.filter = new PropertyFilter(filterText, categoryText, propertyId, this.label);
            this.comboBox1.SelectedIndexChanged += textChanged;
        }

        public String getValue()
        {
            return ListPropertyValues.getInvariantValueForLabel(propertyId, this.availableValues[this.comboBox1.SelectedIndex]);
        }

        public void button1_Click(object sender, EventArgs e)
        {
            if (originalValue != defaultValue)
            {
                PropertiesForm.hasChanges = true;
            }
            this.comboBox1.SelectedIndex = availableValues.IndexOf(defaultValue);
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (this.availableValues[this.comboBox1.SelectedIndex] != originalValue)
            {
                PropertiesForm.hasChanges = true;
            }
        }
    }
}
