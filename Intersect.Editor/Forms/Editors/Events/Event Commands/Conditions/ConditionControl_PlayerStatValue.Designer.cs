using DarkUI.Controls;
using System.ComponentModel;
using System.Windows.Forms;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands.Conditions;

partial class ConditionControl_PlayerStatValue
{
    private IContainer components = null!;
    private DarkGroupBox grpPlayerStat;
    private Label lblStat;
    private DarkComboBox cmbStat;
    private Label lblComparator;
    private DarkComboBox cmbComparator;
    private Label lblValue;
    private DarkNumericUpDown nudValue;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        grpPlayerStat = new DarkGroupBox();
        lblStat = new Label();
        cmbStat = new DarkComboBox();
        lblComparator = new Label();
        cmbComparator = new DarkComboBox();
        lblValue = new Label();
        nudValue = new DarkNumericUpDown();
        grpPlayerStat.SuspendLayout();
        ((ISupportInitialize)nudValue).BeginInit();
        SuspendLayout();
        // 
        // grpPlayerStat
        // 
        grpPlayerStat.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
        grpPlayerStat.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
        grpPlayerStat.Controls.Add(lblStat);
        grpPlayerStat.Controls.Add(cmbStat);
        grpPlayerStat.Controls.Add(lblComparator);
        grpPlayerStat.Controls.Add(cmbComparator);
        grpPlayerStat.Controls.Add(lblValue);
        grpPlayerStat.Controls.Add(nudValue);
        grpPlayerStat.ForeColor = System.Drawing.Color.Gainsboro;
        grpPlayerStat.Location = new System.Drawing.Point(0, 0);
        grpPlayerStat.Name = "grpPlayerStat";
        grpPlayerStat.Size = new Size(350, 110);
        grpPlayerStat.TabIndex = 0;
        grpPlayerStat.TabStop = false;
        grpPlayerStat.Text = "Player Stat";
        // 
        // lblStat
        // 
        lblStat.AutoSize = true;
        lblStat.Location = new System.Drawing.Point(8, 24);
        lblStat.Name = "lblStat";
        lblStat.Size = new System.Drawing.Size(30, 15);
        lblStat.TabIndex = 0;
        lblStat.Text = "Stat:";
        // 
        // cmbStat
        // 
        cmbStat.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
        cmbStat.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
        cmbStat.BorderStyle = ButtonBorderStyle.Solid;
        cmbStat.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
        cmbStat.DrawDropdownHoverOutline = false;
        cmbStat.DrawFocusRectangle = false;
        cmbStat.DrawMode = DrawMode.OwnerDrawFixed;
        cmbStat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStat.FlatStyle = FlatStyle.Flat;
        cmbStat.ForeColor = System.Drawing.Color.Gainsboro;
        cmbStat.Location = new System.Drawing.Point(120, 20);
        cmbStat.Name = "cmbStat";
        cmbStat.Size = new Size(210, 24);
        cmbStat.TabIndex = 1;
        cmbStat.Text = null;
        cmbStat.TextPadding = new Padding(2);
        // 
        // lblComparator
        // 
        lblComparator.AutoSize = true;
        lblComparator.Location = new System.Drawing.Point(8, 54);
        lblComparator.Name = "lblComparator";
        lblComparator.Size = new System.Drawing.Size(74, 15);
        lblComparator.TabIndex = 2;
        lblComparator.Text = "Comparator:";
        // 
        // cmbComparator
        // 
        cmbComparator.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
        cmbComparator.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
        cmbComparator.BorderStyle = ButtonBorderStyle.Solid;
        cmbComparator.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
        cmbComparator.DrawDropdownHoverOutline = false;
        cmbComparator.DrawFocusRectangle = false;
        cmbComparator.DrawMode = DrawMode.OwnerDrawFixed;
        cmbComparator.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbComparator.FlatStyle = FlatStyle.Flat;
        cmbComparator.ForeColor = System.Drawing.Color.Gainsboro;
        cmbComparator.Location = new System.Drawing.Point(120, 50);
        cmbComparator.Name = "cmbComparator";
        cmbComparator.Size = new Size(210, 24);
        cmbComparator.TabIndex = 3;
        cmbComparator.Text = null;
        cmbComparator.TextPadding = new Padding(2);
        // 
        // lblValue
        // 
        lblValue.AutoSize = true;
        lblValue.Location = new System.Drawing.Point(8, 84);
        lblValue.Name = "lblValue";
        lblValue.Size = new System.Drawing.Size(38, 15);
        lblValue.TabIndex = 4;
        lblValue.Text = "Value:";
        // 
        // nudValue
        // 
        nudValue.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
        nudValue.ForeColor = System.Drawing.Color.Gainsboro;
        nudValue.Location = new System.Drawing.Point(120, 82);
        nudValue.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        nudValue.Name = "nudValue";
        nudValue.Size = new Size(120, 23);
        nudValue.TabIndex = 5;
        nudValue.Value = new decimal(new int[] { 0, 0, 0, 0 });
        // 
        // ConditionControl_PlayerStatValue
        // 
        BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
        Controls.Add(grpPlayerStat);
        Name = "ConditionControl_PlayerStatValue";
        Size = new Size(350, 110);
        grpPlayerStat.ResumeLayout(false);
        grpPlayerStat.PerformLayout();
        ((ISupportInitialize)nudValue).EndInit();
        ResumeLayout(false);
    }
}
