using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV4
{
    public partial class PropertiesForm : Form
    {
        public static Boolean hasChanges;

        private Timer searchTimer;
        private const string DEFAULT_SEARCH_TEXT = "Search for property (Ctrl+E)";
        private readonly TimeSpan AUTO_SEARCH_DELAY_SPAN = TimeSpan.FromMilliseconds(500);
        private string searchTextPrev = DEFAULT_SEARCH_TEXT;
        private DateTime nextPrefsRefreshAttemptTime = DateTime.MinValue;

        System.Windows.Forms.Form parent;
        public PropertiesForm(System.Windows.Forms.Form parent)
        {
            hasChanges = false;
            this.parent = parent;
            InitializeComponent();
            if (System.Diagnostics.Debugger.IsAttached) {
                this.button1.Text = "Save (manual restart required)";
            }
            int widgetCount = 0;
            foreach (SettingsProperty strProp in UserSettings.GetUserSettings().getProperties(typeof(String), null, null))
            {
                this.flowLayoutPanel1.Controls.Add(new StringPropertyControl(strProp.Name, Configuration.getUIString(strProp.Name) + " " + Configuration.getUIString("text_prop_type"),
                   UserSettings.GetUserSettings().getString(strProp.Name), (String)strProp.DefaultValue,
                   Configuration.getUIString(strProp.Name + "_help")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), "enable", null))
            {
                Boolean defaultValue;
                Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                this.flowLayoutPanel1.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                    UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                    Configuration.getUIString(boolProp.Name + "_help")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), "frequency", null))
            {
                int defaultValue;
                int.TryParse((String)intProp.DefaultValue, out defaultValue);
                this.flowLayoutPanel1.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                    UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                    Configuration.getUIString(intProp.Name + "_help")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), null, "enable"))
            {
                Boolean defaultValue;
                Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                this.flowLayoutPanel1.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                    UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                    Configuration.getUIString(boolProp.Name + "_help"))); 
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), null, "frequency"))
            {
                int defaultValue;
                int.TryParse((String)intProp.DefaultValue, out defaultValue);
                this.flowLayoutPanel1.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                    UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                    Configuration.getUIString(intProp.Name + "_help")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty floatProp in UserSettings.GetUserSettings().getProperties(typeof(float), null, null))
            {
                float defaultValue;
                float.TryParse((String)floatProp.DefaultValue, out defaultValue);
                this.flowLayoutPanel1.Controls.Add(new FloatPropertyControl(floatProp.Name, Configuration.getUIString(floatProp.Name) + " " + Configuration.getUIString("real_number_prop_type"),
                    UserSettings.GetUserSettings().getFloat(floatProp.Name), defaultValue,
                    Configuration.getUIString(floatProp.Name + "_help"))); 
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;

            this.textBox1.Text = DEFAULT_SEARCH_TEXT;
            this.textBox1.ForeColor = Color.Gray;
            this.textBox1.GotFocus += TextBox1_GotFocus;
            this.textBox1.LostFocus += TextBox1_LostFocus;
            this.textBox1.KeyDown += TextBox1_KeyDown;
            this.button1.Select();

            this.KeyPreview = true;
            this.KeyDown += PropertiesForm_KeyDown;

            this.DoubleBuffered = true;
        }

        public void save()
        {
            foreach (var control in this.flowLayoutPanel1.Controls)
            {
                if (control.GetType() == typeof(StringPropertyControl))
                {
                    StringPropertyControl stringControl = (StringPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(stringControl.propertyId,
                    stringControl.getValue());
                }
                else if (control.GetType() == typeof(IntPropertyControl))
                {
                    IntPropertyControl intControl = (IntPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(intControl.propertyId,
                    intControl.getValue());
                }
                if (control.GetType() == typeof(FloatPropertyControl))
                {
                    FloatPropertyControl floatControl = (FloatPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(floatControl.propertyId,
                    floatControl.getValue());
                }
                if (control.GetType() == typeof(BooleanPropertyControl))
                {
                    BooleanPropertyControl boolControl = (BooleanPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(boolControl.propertyId,
                    boolControl.getValue());
                }
            }
            UserSettings.GetUserSettings().saveUserSettings();
            PropertiesForm.hasChanges = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            save();
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                // have to add "multi" to the start args so the app can restart
                List<String> startArgs = new List<string>();
                startArgs.AddRange(Environment.GetCommandLineArgs());
                if (!startArgs.Contains("multi"))
                {
                    startArgs.Add("multi");
                }
                System.Diagnostics.Process.Start(Application.ExecutablePath, String.Join(" ", startArgs.ToArray())); // to start new instance of application
                parent.Close(); //to turn off current app
            }
        }

        private void pad(int widgetCount)
        {
            int paddedWidgetCount = widgetCount;
            while (paddedWidgetCount % 3 > 0)
            {
                paddedWidgetCount++;
            }
            for (int i = 0; i < paddedWidgetCount - widgetCount; i++)
            {
                this.flowLayoutPanel1.Controls.Add(new Spacer());
            }    
        }

        private void properties_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (PropertiesForm.hasChanges)
            {
                String warningMessage = Configuration.getUIString("save_prop_changes_warning");
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    warningMessage = "You have unsaved changes. Click 'Yes' to save these changes (you will need to manually restart the application). Click 'No' to discard these changes";
                }
                if (MessageBox.Show(warningMessage, Configuration.getUIString("save_changes"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    save();
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        // have to add "multi" to the start args so the app can restart
                        List<String> startArgs = new List<string>();
                        startArgs.AddRange(Environment.GetCommandLineArgs());
                        if (!startArgs.Contains("multi"))
                        {
                            startArgs.Add("multi");
                        }
                        System.Diagnostics.Process.Start(Application.ExecutablePath, String.Join(" ", startArgs.ToArray())); // to start new instance of application
                        parent.Close(); //to turn off current app
                    }
                }
            }           
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.nextPrefsRefreshAttemptTime = DateTime.Now.Add(AUTO_SEARCH_DELAY_SPAN);

            if (this.textBox1.Text == DEFAULT_SEARCH_TEXT)
                return;

            if (this.searchTimer == null)
            {
                this.searchTimer = new Timer();
                this.searchTimer.Interval = 100;
                this.searchTimer.Tick += SearchTimer_Tick;
                this.searchTimer.Start();
            }
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now < this.nextPrefsRefreshAttemptTime)
                return;

            var text = this.textBox1.Text;
            if (text == DEFAULT_SEARCH_TEXT)
            {
                this.searchTextPrev = text;
                return;
            }

            if (text != this.searchTextPrev)
            {
                // This is the case on entering the text box
                if (string.IsNullOrWhiteSpace(text) && this.searchTextPrev != DEFAULT_SEARCH_TEXT)
                    this.PopulatePrefsFiltered(text);
                // General case, new filter.
                else if (!string.IsNullOrWhiteSpace(text))
                    this.PopulatePrefsFiltered(text);

                this.searchTextPrev = text;
            }
        }

        private void TextBox1_GotFocus(object sender, EventArgs e)
        {
            if (this.textBox1.Text == DEFAULT_SEARCH_TEXT)
            {
                this.textBox1.Text = "";
                this.textBox1.ForeColor = Color.Black;
            }
        }

        private void TextBox1_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBox1.Text))
            {
                this.textBox1.Text = DEFAULT_SEARCH_TEXT;
                this.textBox1.ForeColor = Color.Gray;
                this.button1.Select();
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.textBox1.Text = "";
                this.button1.Select();

                if (!string.IsNullOrWhiteSpace(this.searchTextPrev) && this.searchTextPrev != DEFAULT_SEARCH_TEXT) 
                    this.PopulatePrefsFiltered(null);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.searchTextPrev = this.textBox1.Text;
                this.PopulatePrefsFiltered(this.searchTextPrev);
            }
        }

        private void PropertiesForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.E)
                this.textBox1.Select();
        }

        private void PopulatePrefsFiltered(string filter)
        {
            // Unfortunately, stupid piece of shit fucking flow layout panel does not auto resize on hiding controls.
            // I've no idea nor time to keep figuring out wtf is going on, so just remove all the shit but preserve user
            // values.
            this.flowLayoutPanel1.SuspendLayout();
            foreach (var ctrl in this.flowLayoutPanel1.Controls)
            {
                if (ctrl is StringPropertyControl)
                {
                    var spc = ctrl as StringPropertyControl;
                    if (string.IsNullOrWhiteSpace(filter) || spc.propertyId.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                        spc.Visible = true;
                    else
                        spc.Visible = false;
                }
                else if (ctrl is BooleanPropertyControl)
                {
                    var bpc = ctrl as BooleanPropertyControl;
                    if (string.IsNullOrWhiteSpace(filter) || bpc.propertyId.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                        bpc.Visible = true;
                    else
                        bpc.Visible = false;
                }
                else if (ctrl is IntPropertyControl)
                {
                    var ipc = ctrl as IntPropertyControl;
                    if (string.IsNullOrWhiteSpace(filter) || ipc.propertyId.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                        ipc.Visible = true;
                    else
                        ipc.Visible = false;
                }
                else if (ctrl is FloatPropertyControl)
                {
                    var fpc = ctrl as FloatPropertyControl;
                    if (string.IsNullOrWhiteSpace(filter) || fpc.propertyId.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                        fpc.Visible = true;
                    else
                        fpc.Visible = false;
                }
                else if (ctrl is Spacer)
                {
                    var s = ctrl as Spacer;
                    if (string.IsNullOrWhiteSpace(filter))
                        s.Visible = true;
                    else
                        s.Visible = false;
                }
            }
            /*this.flowLayoutPanel1.Controls.Clear();

            int widgetCount = 0;
            foreach (SettingsProperty strProp in UserSettings.GetUserSettings().getProperties(typeof(String), null, null))
            {
                if (string.IsNullOrWhiteSpace(filter) || strProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    this.flowLayoutPanel1.Controls.Add(new StringPropertyControl(strProp.Name, Configuration.getUIString(strProp.Name) + " " + Configuration.getUIString("text_prop_type"),
                       UserSettings.GetUserSettings().getString(strProp.Name), (String)strProp.DefaultValue,
                       Configuration.getUIString(strProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), "enable", null))
            {
                if (string.IsNullOrWhiteSpace(filter) || boolProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    Boolean defaultValue;
                    Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                    this.flowLayoutPanel1.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                        UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                        Configuration.getUIString(boolProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), "frequency", null))
            {
                if (string.IsNullOrWhiteSpace(filter) || intProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    int defaultValue;
                    int.TryParse((String)intProp.DefaultValue, out defaultValue);
                    this.flowLayoutPanel1.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                        UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                        Configuration.getUIString(intProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), null, "enable"))
            {
                if (string.IsNullOrWhiteSpace(filter) || boolProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    Boolean defaultValue;
                    Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                    this.flowLayoutPanel1.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                        UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                        Configuration.getUIString(boolProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), null, "frequency"))
            {
                if (string.IsNullOrWhiteSpace(filter) || intProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    int defaultValue;
                    int.TryParse((String)intProp.DefaultValue, out defaultValue);
                    this.flowLayoutPanel1.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                        UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                        Configuration.getUIString(intProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty floatProp in UserSettings.GetUserSettings().getProperties(typeof(float), null, null))
            {
                if (string.IsNullOrWhiteSpace(filter) || floatProp.Name.ToUpperInvariant().Contains(filter.ToUpperInvariant()))
                {
                    float defaultValue;
                    float.TryParse((String)floatProp.DefaultValue, out defaultValue);
                    this.flowLayoutPanel1.Controls.Add(new FloatPropertyControl(floatProp.Name, Configuration.getUIString(floatProp.Name) + " " + Configuration.getUIString("real_number_prop_type"),
                        UserSettings.GetUserSettings().getFloat(floatProp.Name), defaultValue,
                        Configuration.getUIString(floatProp.Name + "_help")));
                    widgetCount++;
                }
            }
            pad(widgetCount);
            widgetCount = 0;

            if (this.flowLayoutPanel1.Controls.Count == 0)
                this.flowLayoutPanel1.Controls.Add(new Label() { Text = "Nothing found." }); 
                */
            this.flowLayoutPanel1.ResumeLayout();
            //this.flowLayoutPanel1.PerformLayout();
        }
    }
}
