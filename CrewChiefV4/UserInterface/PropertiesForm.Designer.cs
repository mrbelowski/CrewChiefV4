using System;

namespace CrewChiefV4
{
    partial class PropertiesForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.saveButton = new System.Windows.Forms.Button();
            this.propertiesFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.exitButton = new System.Windows.Forms.Button();
            this.restoreButton = new System.Windows.Forms.Button();
            this.headerTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.searchBoxTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.gameFilterLabel = new System.Windows.Forms.Label();
            this.filterBox = new System.Windows.Forms.ComboBox();
            this.showCommonCheckbox = new System.Windows.Forms.CheckBox();
            this.mainTableLayoutPanel.SuspendLayout();
            this.buttonsTableLayoutPanel.SuspendLayout();
            this.headerTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            // 
            // saveButton
            // 
            this.saveButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.saveButton.Location = new System.Drawing.Point(3, 3);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(159, 40);
            this.saveButton.Text = Configuration.getUIString("save_and_restart");
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            this.saveButton.TabIndex = 1;
            // 
            // preferencesFlowLayoutPanel
            // 
            this.propertiesFlowLayoutPanel.AutoScroll = true;
            this.propertiesFlowLayoutPanel.AutoSize = true;
            this.propertiesFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.propertiesFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertiesFlowLayoutPanel.Location = new System.Drawing.Point(3, 31);
            this.propertiesFlowLayoutPanel.Name = "preferencesFlowLayoutPanel";
            this.propertiesFlowLayoutPanel.Size = new System.Drawing.Size(969, 612);
            this.propertiesFlowLayoutPanel.TabIndex = 0;
            this.propertiesFlowLayoutPanel.TabStop = true;
            //
            // gameFilterLablel
            //
            this.gameFilterLabel.Name = "gameFilterLabel";
            this.gameFilterLabel.Text = Configuration.getUIString("game_filter_label");
            this.gameFilterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.gameFilterLabel.TabIndex = 9;
            //
            // filterBox
            //
            this.filterBox.Name = "filterBox";
            this.filterBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterBox.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.filterBox.MinimumSize = new System.Drawing.Size(203, -1);
            this.filterBox.MaximumSize = new System.Drawing.Size(203, -1);
            this.filterBox.SelectedValueChanged += FilterBox_SelectedValueChanged;
            this.filterBox.TabIndex = 10;
            //
            // showCommonCheckbox
            //
            this.showCommonCheckbox.Name = "showCommonCheckbox";
            this.showCommonCheckbox.Text = Configuration.getUIString("show_common_props_label");
            this.showCommonCheckbox.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.showCommonCheckbox.Margin = new System.Windows.Forms.Padding(10, 5, 3, 3);
            this.showCommonCheckbox.Checked = true;
            this.showCommonCheckbox.CheckedChanged += ShowCommonCheckbox_CheckedChanged;
            // 
            // searchTextBox
            // 
            var tooltip = Configuration.getUIString("search_box_tooltip_line1") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line2") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line4") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line5") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line6") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line7") + Environment.NewLine
                + Configuration.getUIString("search_box_tooltip_line8") + Environment.NewLine;
            this.searchTextBox.Location = new System.Drawing.Point(780, 3);
            this.searchTextBox.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(189, 20);
            this.searchTextBox.TabIndex = 12;
            this.searchBoxTooltip.SetToolTip(this.searchTextBox, tooltip);
            this.searchTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 1;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.headerTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.propertiesFlowLayoutPanel, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.buttonsTableLayoutPanel, 0, 2);
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 3;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(975, 703);
            // 
            // buttonsTableLayoutPanel
            // 
            this.buttonsTableLayoutPanel.ColumnCount = 4;
            this.buttonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17F));
            this.buttonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17F));
            this.buttonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 49F));
            this.buttonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17F));
            this.buttonsTableLayoutPanel.Controls.Add(this.saveButton, 0, 0);
            this.buttonsTableLayoutPanel.Controls.Add(this.exitButton, 1, 0);
            this.buttonsTableLayoutPanel.Controls.Add(this.restoreButton, 3, 0);
            this.buttonsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsTableLayoutPanel.Location = new System.Drawing.Point(0, 646);
            this.buttonsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.buttonsTableLayoutPanel.Name = "buttonsTableLayoutPanel";
            this.buttonsTableLayoutPanel.RowCount = 1;
            this.buttonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonsTableLayoutPanel.Size = new System.Drawing.Size(975, 57);
            this.buttonsTableLayoutPanel.TabIndex = 0;
            // 
            // exitButton
            // 
            this.exitButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.exitButton.Location = new System.Drawing.Point(168, 3);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(159, 40);
            this.exitButton.Text = Configuration.getUIString("exit_without_saving");
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            this.exitButton.TabIndex = 0;
            // 
            // restoreButton
            // 
            this.restoreButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.restoreButton.Location = new System.Drawing.Point(810, 3);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(162, 40);
            this.restoreButton.Text = Configuration.getUIString("restore_default_settings");
            this.restoreButton.UseVisualStyleBackColor = true;
            this.restoreButton.Click += new System.EventHandler(this.restoreButton_Click);
            this.restoreButton.TabIndex = 2;
            // 
            // headerTableLayoutPanel
            // 
            this.headerTableLayoutPanel.ColumnCount = 5;
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29F));
            this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.headerTableLayoutPanel.Controls.Add(this.gameFilterLabel, 0, 0);
            this.headerTableLayoutPanel.Controls.Add(this.filterBox, 1, 0);
            this.headerTableLayoutPanel.Controls.Add(this.showCommonCheckbox, 2, 0);
            this.headerTableLayoutPanel.Controls.Add(this.searchTextBox, 4, 0);
            this.headerTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.headerTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.headerTableLayoutPanel.Name = "headerTableLayoutPanel";
            this.headerTableLayoutPanel.RowCount = 1;
            this.headerTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerTableLayoutPanel.Size = new System.Drawing.Size(975, 28);
            this.headerTableLayoutPanel.TabIndex = 0;
            // 
            // searchBoxTooltip
            // 
            this.searchBoxTooltip.AutoPopDelay = 5000;
            this.searchBoxTooltip.InitialDelay = 250;
            this.searchBoxTooltip.IsBalloon = true;
            this.searchBoxTooltip.ReshowDelay = 100;
            // 
            // PropertiesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(975, 703);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "PropertiesForm";
            this.Text = Configuration.getUIString("properties_form");
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.properties_FormClosing);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.buttonsTableLayoutPanel.ResumeLayout(false);
            this.headerTableLayoutPanel.ResumeLayout(false);
            this.headerTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.FlowLayoutPanel propertiesFlowLayoutPanel;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel headerTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel buttonsTableLayoutPanel;
        private System.Windows.Forms.ToolTip searchBoxTooltip;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Button restoreButton;
        private System.Windows.Forms.Label gameFilterLabel;
        private System.Windows.Forms.ComboBox filterBox;
        private System.Windows.Forms.CheckBox showCommonCheckbox;
    }
}