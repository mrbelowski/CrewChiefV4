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
    /// <summary>
    /// this class holds the list box label -> invariant value mappings for each listbox property used in the properties
    /// form. 
    /// </summary>
    public class ListPropertyValues
    {
        public static void addPropertyToListboxData(string propertyName, string valueEnumTypeName)
        {
            listBoxData.Add(propertyName, getListBoxItemsForEnum(propertyName, Type.GetType(valueEnumTypeName, true)));

            // Note that it's also possible to hard code the contents of a listbox here if it's not backed by an enum, by getting items manually - e.g.
            // listBoxData.Add("interrupt_setting_listprop", new ListBoxItem[]{
            //    new ListBoxItem(Configuration.getUIString("ui_text_for_item_1"), "invariant_value_for_item_1"),
            //    new ListBoxItem(Configuration.getUIString("ui_text_for_item_2"), "invariant_value_for_item_2")
            // }.ToList());
        }

        /// <summary>
        /// 
        /// Convenience method to populate a listbox backed by an enum. The ui_text.txt file must contain each item's label
        /// as property_name_listprop_value_n where n is the position in the specified enum type. The set must be contiguous
        /// and complete WRT to the enum declaration
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="enumType"></param>
        /// <returns></returns>
        private static List<ListBoxItem> getListBoxItemsForEnum(String propertyName, Type enumType)
        {
            List<ListBoxItem> itemsList = new List<ListBoxItem>();
            Boolean gotValue = false;
            int index = 0;
            do
            {
                String label = Configuration.getUIStringStrict(propertyName + "_value_" + index);
                if (label != null)
                {
                    itemsList.Add(new ListBoxItem(label, Enum.GetName(enumType, index)));
                    gotValue = true;
                    index++;
                }
                else
                {
                    gotValue = false;
                }
            }
            while (gotValue);
            return itemsList;
        }

        /// <summary>
        /// A single list box item with the language specific label for this item, and its invariant value.
        /// The invariant value will typically be parsed to an enum
        /// </summary>
        public class ListBoxItem
        {
            public String label;
            public String invariantValue;
            public ListBoxItem(String label, String invariantValue)
            {
                this.label = label;
                this.invariantValue = invariantValue;
            }
        }

        public static Dictionary<String, List<ListBoxItem>> listBoxData = new Dictionary<string, List<ListBoxItem>>();

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
            String defaultValue, String helpText, String filterText, String categoryText, String propertyType)
        {
            InitializeComponent();

            this.label = label;
            this.propertyId = propertyId;
            this.label1.Text = label;
            ListPropertyValues.addPropertyToListboxData(propertyId, propertyType);
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
