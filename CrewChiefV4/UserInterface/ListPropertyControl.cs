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
        static ListPropertyValues()
        {
            // listbox items for interrupt property
            ListBoxItem[] interruptItems = new ListBoxItem[]{
                new ListBoxItem(Configuration.getUIString("interrupts_none"), "NONE"),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_only"), CrewChiefV4.Audio.SoundType.SPOTTER.ToString()),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_and_critical"), CrewChiefV4.Audio.SoundType.CRITICAL_MESSAGE.ToString()),
                new ListBoxItem(Configuration.getUIString("interrupts_spotter_critical_and_important"), CrewChiefV4.Audio.SoundType.IMPORTANT_MESSAGE.ToString())
            };
            listBoxData.Add("LISTBOX_interrupt_setting", interruptItems.ToList());

            // listbox items for TTS property
            ListBoxItem[] ttsItems = new ListBoxItem[]{
                new ListBoxItem(Configuration.getUIString("tts_never"), CrewChiefV4.Audio.AudioPlayer.TTS_OPTION.NEVER.ToString()),
                new ListBoxItem(Configuration.getUIString("tts_only_when_necessary"), CrewChiefV4.Audio.AudioPlayer.TTS_OPTION.ONLY_WHEN_NECESSARY.ToString()),
                new ListBoxItem(Configuration.getUIString("tts_any_time"), CrewChiefV4.Audio.AudioPlayer.TTS_OPTION.ANY_TIME.ToString())
            };
            listBoxData.Add("LISTBOX_tts_setting", ttsItems.ToList());
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
