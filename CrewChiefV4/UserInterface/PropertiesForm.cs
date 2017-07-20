﻿using System;
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
    }
}
