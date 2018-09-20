using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
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

        private Timer searchTimer;
        private readonly string DEFAULT_SEARCH_TEXT = Configuration.getUIString("search_box_default_text");
        private readonly TimeSpan AUTO_SEARCH_DELAY_SPAN = TimeSpan.FromMilliseconds(700);
        private DateTime nextPrefsRefreshAttemptTime = DateTime.MinValue;
        private Label noMatchedLabel = new Label() { Text = Configuration.getUIString("no_matches") };

        public static String listPropPostfix = "_listprop";

        private string searchTextPrev = null;
        private GameEnum gameFilterPrev = GameEnum.UNKNOWN;

        internal enum SpecialFilter
        {
            ALL_PREFERENCES = GameEnum.UNKNOWN + 1,
            COMMON_PREFERENCES,
            UNKNOWN
        }
        private SpecialFilter specialFilterPrev = SpecialFilter.UNKNOWN;
        private bool includeCommonPreferencesPrev = true;
        
        internal enum PropertyCategory
        {
            ALL,  // Don't assign this to properties, this means no filtering applied.
            UI_STARTUP_AND_PATHS,
            AUDIO_VOICE_AND_CONTROLLERS,
            SPOTTER,
            FLAGS_AND_RULES,
            MESSAGE_FREQUENCES,
            FUEL_TEMPS_AND_DAMAGES,
            TIMINGS,
            PIT_STOPS_AND_MULTICLASS,
            MISC,  // Implied by default.
            UNKNOWN
        }
        private PropertyCategory categoryFilterPrev = PropertyCategory.UNKNOWN;

        public class ComboBoxItem<T>
        {
            public string Label { get; set; }
            public T Value { get; set; }

            public override string ToString()
            {
                return this.Label != null ? this.Label : string.Empty;
            }
        }

        // Note: vast majority of startup time is in ShowDialog.  Looks like pretty much the only way to speed it up is by reducing
        // number of controls or splitting in tabs.
        public PropertiesForm(System.Windows.Forms.Form parent)
        {
            if (MainWindow.forceMinWindowSize)
            {
                this.MinimumSize = new System.Drawing.Size(995, 745);
            }

            hasChanges = false;
            this.parent = parent;

            InitializeComponent();
            if (CrewChief.Debugging)
            {
                this.saveButton.Text = "Save (manual restart required)";
            }

            this.SuspendLayout();
            this.propertiesFlowLayoutPanel.SuspendLayout();

            int widgetCount = 0;
            foreach (SettingsProperty strProp in UserSettings.GetUserSettings().getProperties(typeof(String), null, null))
            {
                if (strProp.Name.EndsWith(PropertiesForm.listPropPostfix) && ListPropertyValues.getListBoxLabels(strProp.Name) != null)
                {
                    this.propertiesFlowLayoutPanel.Controls.Add(new ListPropertyControl(strProp.Name, Configuration.getUIString(strProp.Name) + " " + Configuration.getUIString("text_prop_type"),
                       UserSettings.GetUserSettings().getString(strProp.Name), (String)strProp.DefaultValue,
                       Configuration.getUIString(strProp.Name + "_help"), Configuration.getUIStringStrict(strProp.Name + "_filter"),
                       Configuration.getUIStringStrict(strProp.Name + "_category"), Configuration.getUIStringStrict(strProp.Name + "_type")));
                }
                else
                {
                    this.propertiesFlowLayoutPanel.Controls.Add(new StringPropertyControl(strProp.Name, Configuration.getUIString(strProp.Name) + " " + Configuration.getUIString("text_prop_type"),
                       UserSettings.GetUserSettings().getString(strProp.Name), (String)strProp.DefaultValue,
                       Configuration.getUIString(strProp.Name + "_help"), Configuration.getUIStringStrict(strProp.Name + "_filter"),
                       Configuration.getUIStringStrict(strProp.Name + "_category")));
                }
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), "enable", null))
            {
                Boolean defaultValue;
                Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                this.propertiesFlowLayoutPanel.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                    UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                    Configuration.getUIString(boolProp.Name + "_help"), Configuration.getUIStringStrict(boolProp.Name + "_filter"),
                    Configuration.getUIStringStrict(boolProp.Name + "_category")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), "frequency", null))
            {
                int defaultValue;
                int.TryParse((String)intProp.DefaultValue, out defaultValue);
                this.propertiesFlowLayoutPanel.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                    UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                    Configuration.getUIString(intProp.Name + "_help"), Configuration.getUIStringStrict(intProp.Name + "_filter"),
                    Configuration.getUIStringStrict(intProp.Name + "_category")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty boolProp in UserSettings.GetUserSettings().getProperties(typeof(Boolean), null, "enable"))
            {
                Boolean defaultValue;
                Boolean.TryParse((String)boolProp.DefaultValue, out defaultValue);
                this.propertiesFlowLayoutPanel.Controls.Add(new BooleanPropertyControl(boolProp.Name, Configuration.getUIString(boolProp.Name) + " " + Configuration.getUIString("boolean_prop_type"),
                    UserSettings.GetUserSettings().getBoolean(boolProp.Name), defaultValue,
                    Configuration.getUIString(boolProp.Name + "_help"), Configuration.getUIStringStrict(boolProp.Name + "_filter"),
                    Configuration.getUIStringStrict(boolProp.Name + "_category"))); 
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty intProp in UserSettings.GetUserSettings().getProperties(typeof(int), null, "frequency"))
            {
                int defaultValue;
                int.TryParse((String)intProp.DefaultValue, out defaultValue);
                this.propertiesFlowLayoutPanel.Controls.Add(new IntPropertyControl(intProp.Name, Configuration.getUIString(intProp.Name) + " " + Configuration.getUIString("integer_prop_type"),
                    UserSettings.GetUserSettings().getInt(intProp.Name), defaultValue,
                    Configuration.getUIString(intProp.Name + "_help"), Configuration.getUIStringStrict(intProp.Name + "_filter"),
                    Configuration.getUIStringStrict(intProp.Name + "_category")));
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;
            foreach (SettingsProperty floatProp in UserSettings.GetUserSettings().getProperties(typeof(float), null, null))
            {
                float defaultValue;
                float.TryParse((String)floatProp.DefaultValue, out defaultValue);
                this.propertiesFlowLayoutPanel.Controls.Add(new FloatPropertyControl(floatProp.Name, Configuration.getUIString(floatProp.Name) + " " + Configuration.getUIString("real_number_prop_type"),
                    UserSettings.GetUserSettings().getFloat(floatProp.Name), defaultValue,
                    Configuration.getUIString(floatProp.Name + "_help"), Configuration.getUIStringStrict(floatProp.Name + "_filter"),
                    Configuration.getUIStringStrict(floatProp.Name + "_category"))); 
                widgetCount++;
            }
            pad(widgetCount);
            widgetCount = 0;

            this.searchTextPrev = DEFAULT_SEARCH_TEXT;
            this.gameFilterPrev = GameEnum.UNKNOWN;
            this.specialFilterPrev = SpecialFilter.UNKNOWN;
            this.categoryFilterPrev = PropertyCategory.ALL;  // Initialize this here, so that initial game filtering works.
            this.includeCommonPreferencesPrev = true;

            this.searchTextBox.Text = DEFAULT_SEARCH_TEXT;
            this.searchTextBox.ForeColor = Color.Gray;
            this.searchTextBox.GotFocus += SearchTextBox_GotFocus;
            this.searchTextBox.LostFocus += SearchTextBox_LostFocus;
            this.searchTextBox.KeyDown += SearchTextBox_KeyDown;
            this.exitButton.Select();

            this.KeyPreview = true;
            this.KeyDown += PropertiesForm_KeyDown;

            this.DoubleBuffered = true;

            // Filtering setup.
            this.filterBox.Items.Clear();
            this.filterBox.Items.Add(new ComboBoxItem<SpecialFilter>()
            {
                Label = Configuration.getUIString("all_preferences_label"),
                Value = SpecialFilter.ALL_PREFERENCES
            });

            this.filterBox.Items.Add(new ComboBoxItem<SpecialFilter>()
            {
                Label = Configuration.getUIString("common_preferences_label"),
                Value = SpecialFilter.COMMON_PREFERENCES
            });

            lock (MainWindow.instanceLock)
            {
                if (MainWindow.instance != null)
                {
                    var currSelectedGameFriendlyName = MainWindow.instance.gameDefinitionList.Text;
                    foreach (var game in MainWindow.instance.gameDefinitionList.Items)
                    {
                        var friendlyGameName = game.ToString();
                        this.filterBox.Items.Add(new ComboBoxItem<GameEnum>()
                        {
                            Label = friendlyGameName,
                            Value = GameDefinition.getGameDefinitionForFriendlyName(friendlyGameName).gameEnum
                        });

                        if (friendlyGameName == currSelectedGameFriendlyName)
                            this.filterBox.SelectedIndex = this.filterBox.Items.Count - 1;
                    }
                }
            }

            // Special case for no game selected.
            if (this.filterBox.SelectedIndex == -1)
            {
                this.filterBox.SelectedIndex = 0;
                // No need to filter.
                this.specialFilterPrev = SpecialFilter.ALL_PREFERENCES;
            }
            
            // Category filter:
            this.categoriesBox.Items.Clear();
            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("all_categories_label"),
                Value = PropertyCategory.ALL
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("ui_startup_and_paths_category_label"),
                Value = PropertyCategory.UI_STARTUP_AND_PATHS
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("audio_voice_and_controllers_category_label"),
                Value = PropertyCategory.AUDIO_VOICE_AND_CONTROLLERS
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("spotter_category_label"),
                Value = PropertyCategory.SPOTTER
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("flags_and_rules_category_label"),
                Value = PropertyCategory.FLAGS_AND_RULES
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("message_frequences_category_label"),
                Value = PropertyCategory.MESSAGE_FREQUENCES
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("fuel_temps_and_damages_category_label"),
                Value = PropertyCategory.FUEL_TEMPS_AND_DAMAGES
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("timings_category_label"),
                Value = PropertyCategory.TIMINGS
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("pit_stops_and_multiclass_category_label"),
                Value = PropertyCategory.PIT_STOPS_AND_MULTICLASS
            });

            this.categoriesBox.Items.Add(new ComboBoxItem<PropertyCategory>()
            {
                Label = Configuration.getUIString("misc_category_label"),
                Value = PropertyCategory.MISC
            });

            this.categoriesBox.SelectedIndex = 0;

            this.categoriesBox.SelectedValueChanged += this.CategoriesBox_SelectedValueChanged;

            this.propertiesFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public void save()
        {
            foreach (var control in this.propertiesFlowLayoutPanel.Controls)
            {
                if (control.GetType() == typeof(StringPropertyControl))
                {
                    StringPropertyControl stringControl = (StringPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(stringControl.propertyId,
                    stringControl.getValue());
                }
                else if (control.GetType() == typeof(ListPropertyControl))
                {
                    ListPropertyControl listControl = (ListPropertyControl)control;
                    UserSettings.GetUserSettings().setProperty(listControl.propertyId,
                    listControl.getValue());
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

        private void saveButton_Click(object sender, EventArgs e)
        {
            save();
            if (!CrewChief.Debugging)
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
                this.propertiesFlowLayoutPanel.Controls.Add(new Spacer());
            }    
        }

        private void properties_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.searchTimer != null)
            {
                this.searchTimer.Stop();
                this.searchTimer = null;
            }

            if (PropertiesForm.hasChanges)
            {
                String warningMessage = Configuration.getUIString("save_prop_changes_warning");
                if (CrewChief.Debugging)
                {
                    warningMessage = "You have unsaved changes. Click 'Yes' to save these changes (you will need to manually restart the application). Click 'No' to discard these changes";
                }
                if (MessageBox.Show(warningMessage, Configuration.getUIString("save_changes"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    save();
                    if (!CrewChief.Debugging)
                    {
                        // have to add "multi" to the start args so the app can restart
                        List<String> startArgs = new List<string>();
                        startArgs.AddRange(Environment.GetCommandLineArgs());
                        if (!startArgs.Contains("multi"))
                        {
                            startArgs.Add("multi");
                        }
                        System.Diagnostics.Process.Start(Application.ExecutablePath, String.Join(" ", startArgs.ToArray())); // to start new instance of application
                        parent.Close(); // To turn off current app
                    }
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.nextPrefsRefreshAttemptTime = DateTime.UtcNow.Add(AUTO_SEARCH_DELAY_SPAN);

            if (this.searchTextBox.Text == DEFAULT_SEARCH_TEXT)
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
            if (DateTime.UtcNow < this.nextPrefsRefreshAttemptTime)
                return;

            var text = this.searchTextBox.Text;
            if (text == DEFAULT_SEARCH_TEXT)
            {
                this.searchTextPrev = text;
                return;
            }

            if (text != this.searchTextPrev)
            {
                // This is the case of clearing previously non-empty search
                if (string.IsNullOrWhiteSpace(text))
                    this.PopulatePrefsFiltered("", this.gameFilterPrev, this.specialFilterPrev, this.includeCommonPreferencesPrev, this.categoryFilterPrev);  // Clear filter out.
                // General case, new filter.
                else if (!string.IsNullOrWhiteSpace(text))
                    this.PopulatePrefsFiltered(text, this.gameFilterPrev, this.specialFilterPrev, this.includeCommonPreferencesPrev, this.categoryFilterPrev);  // Apply new filter.

                this.searchTextPrev = text;
            }
        }

        private void FilterBox_SelectedValueChanged(object sender, EventArgs e)
        {
            var gameFilter = GameEnum.UNKNOWN;
            var specialFilter = SpecialFilter.UNKNOWN;
            if (this.filterBox.SelectedItem is ComboBoxItem<GameEnum>)
            {
                // Game filter selected.
                gameFilter = (this.filterBox.SelectedItem as ComboBoxItem<GameEnum>).Value;
                this.showCommonCheckbox.Enabled = true;
            }
            else
            {
                // Special filter selected.
                specialFilter = (this.filterBox.SelectedItem as ComboBoxItem<SpecialFilter>).Value;
                this.showCommonCheckbox.Enabled = false;
            }

            if ((gameFilter != GameEnum.UNKNOWN && gameFilter != this.gameFilterPrev)
                || (specialFilter != SpecialFilter.UNKNOWN && specialFilter != this.specialFilterPrev))
            {
                this.PopulatePrefsFiltered(this.searchTextPrev == this.DEFAULT_SEARCH_TEXT ? "" : this.searchTextPrev, gameFilter, specialFilter, this.includeCommonPreferencesPrev, this.categoryFilterPrev);

                // Save filter values but keep gameFilter and specialFilter mutually exclusive.
                if (gameFilter != GameEnum.UNKNOWN)
                {
                    this.gameFilterPrev = gameFilter;
                    this.specialFilterPrev = SpecialFilter.UNKNOWN;
                }

                if (specialFilter != SpecialFilter.UNKNOWN)
                {
                    this.specialFilterPrev = specialFilter;
                    this.gameFilterPrev = GameEnum.UNKNOWN;
                }
            }
        }

        private void CategoriesBox_SelectedValueChanged(object sender, EventArgs e)
        {
            var categoryFilter = (this.categoriesBox.SelectedItem as ComboBoxItem<PropertyCategory>).Value;
            if (categoryFilter != this.categoryFilterPrev)
            {
                this.PopulatePrefsFiltered(this.searchTextPrev == this.DEFAULT_SEARCH_TEXT ? "" : this.searchTextPrev, this.gameFilterPrev,
                    this.specialFilterPrev, this.includeCommonPreferencesPrev, categoryFilter);

                this.categoryFilterPrev = categoryFilter;
            }

        }

        private void ShowCommonCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var showCommon = this.showCommonCheckbox.Checked;
            if (showCommon != this.includeCommonPreferencesPrev)
            {
                this.PopulatePrefsFiltered(this.searchTextPrev == this.DEFAULT_SEARCH_TEXT ? "" : this.searchTextPrev, this.gameFilterPrev, this.specialFilterPrev, showCommon, this.categoryFilterPrev);
                this.includeCommonPreferencesPrev = showCommon;
            }
        }


        private void SearchTextBox_GotFocus(object sender, EventArgs e)
        {
            if (this.searchTextBox.Text == DEFAULT_SEARCH_TEXT)
            {
                this.searchTextBox.Text = "";
                this.searchTextBox.ForeColor = Color.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.searchTextBox.Text))
            {
                this.searchTextBox.Text = DEFAULT_SEARCH_TEXT;
                this.searchTextBox.ForeColor = Color.Gray;

                // Not sure why I had this like that, ever.  Keep commented out for now.
                //this.exitButton.Select();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.searchTextBox.Select();
                this.searchTextBox.Text = "";
                this.exitButton.Select();

                if (!string.IsNullOrWhiteSpace(this.searchTextPrev) && this.searchTextPrev != DEFAULT_SEARCH_TEXT) 
                    this.PopulatePrefsFiltered(null, this.gameFilterPrev, this.specialFilterPrev, this.includeCommonPreferencesPrev, this.categoryFilterPrev);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.searchTextPrev = this.searchTextBox.Text;
                this.PopulatePrefsFiltered(this.searchTextPrev, this.gameFilterPrev, this.specialFilterPrev, this.includeCommonPreferencesPrev, this.categoryFilterPrev);
            }
        }

        
        private void PropertiesForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.E)
                this.searchTextBox.Select();
            else if (e.KeyCode == Keys.Escape)
            {
                // Close only if no search is active.
                if (this.searchTextBox.Text == DEFAULT_SEARCH_TEXT)
                    this.Close();
                else
                    this.SearchTextBox_KeyDown(sender, e); // Otherwise, forward.
            }
        }

        private void PopulatePrefsFiltered(string filter, GameEnum gameFilter, SpecialFilter specialFilter, bool includeCommon, PropertyCategory categoryFilter)
        {
            this.SuspendLayout();
            this.propertiesFlowLayoutPanel.SuspendLayout();

            var anyHits = false;
            var filterUpper = string.IsNullOrWhiteSpace(filter) ? filter : filter.ToUpperInvariant();
            foreach (var ctrl in this.propertiesFlowLayoutPanel.Controls)
            {
                if (ctrl is StringPropertyControl)
                {
                    var spc = ctrl as StringPropertyControl;
                    if (spc.filter.Applies(filterUpper, gameFilter, specialFilter, includeCommon, categoryFilter))
                    {
                        spc.Visible = true;
                        anyHits = true;
                    }
                    else
                        spc.Visible = false;
                }
                else if (ctrl is BooleanPropertyControl)
                {
                    var bpc = ctrl as BooleanPropertyControl;
                    if (bpc.filter.Applies(filterUpper, gameFilter, specialFilter, includeCommon, categoryFilter))
                    {
                        bpc.Visible = true;
                        anyHits = true;
                    }
                    else
                        bpc.Visible = false;
                }
                else if (ctrl is IntPropertyControl)
                {
                    var ipc = ctrl as IntPropertyControl;
                    if (ipc.filter.Applies(filterUpper, gameFilter, specialFilter, includeCommon, categoryFilter))
                    {
                        ipc.Visible = true;
                        anyHits = true;
                    }
                    else
                        ipc.Visible = false;
                }
                else if (ctrl is FloatPropertyControl)
                {
                    var fpc = ctrl as FloatPropertyControl;
                    if (fpc.filter.Applies(filterUpper, gameFilter, specialFilter, includeCommon, categoryFilter))
                    {
                        fpc.Visible = true;
                        anyHits = true;
                    }
                    else
                        fpc.Visible = false;
                }
                else if (ctrl is ListPropertyControl)
                {
                    var lpc = ctrl as ListPropertyControl;
                    if (lpc.filter.Applies(filterUpper, gameFilter, specialFilter, includeCommon, categoryFilter))
                    {
                        lpc.Visible = true;
                        anyHits = true;
                    }
                    else
                        lpc.Visible = false;
                }
                else if (ctrl is Spacer)
                {
                    var s = ctrl as Spacer;
                    if (!string.IsNullOrWhiteSpace(filterUpper)
                        || gameFilter != GameEnum.UNKNOWN
                        || specialFilter != SpecialFilter.ALL_PREFERENCES
                        || categoryFilter != PropertyCategory.ALL)
                        s.Visible = false;  // If any filtering is applied, hide splitters.
                    else
                        s.Visible = true;
                }
            }

            if (!anyHits)
                this.propertiesFlowLayoutPanel.Controls.Add(this.noMatchedLabel);
            else
                this.propertiesFlowLayoutPanel.Controls.Remove(this.noMatchedLabel);

            this.propertiesFlowLayoutPanel.ResumeLayout();
            this.ResumeLayout();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void restoreButton_Click(object sender, EventArgs e)
        {
            // Note that even after this said yes, there's still "Save" step.  Maybe dialog isn't necessary.
            var result = MessageBox.Show(Configuration.getUIString("reset_warning_text"), Configuration.getUIString("reset_warning_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
                return;

            foreach (var ctrl in this.propertiesFlowLayoutPanel.Controls)
            {
                if (ctrl is StringPropertyControl)
                {
                    var spc = ctrl as StringPropertyControl;
                    spc.button1_Click(sender, e);
                }
                else if (ctrl is ListPropertyControl)
                {
                    var spc = ctrl as ListPropertyControl;
                    spc.button1_Click(sender, e);
                }
                else if (ctrl is BooleanPropertyControl)
                {
                    var bpc = ctrl as BooleanPropertyControl;
                    bpc.button1_Click(sender, e);
                }
                else if (ctrl is IntPropertyControl)
                {
                    var ipc = ctrl as IntPropertyControl;
                    ipc.button1_Click(sender, e);
                }
                else if (ctrl is FloatPropertyControl)
                {
                    var fpc = ctrl as FloatPropertyControl;
                    fpc.button1_Click(sender, e);
                }
            }
        }
    }
}
