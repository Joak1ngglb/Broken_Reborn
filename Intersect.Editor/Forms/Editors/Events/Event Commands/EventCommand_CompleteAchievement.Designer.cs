using DarkUI.Controls;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands
{
    partial class EventCommandCompleteAchievement
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpCompleteAchievement = new DarkUI.Controls.DarkGroupBox();
            this.cmbAchievements = new DarkUI.Controls.DarkComboBox();
            this.lblAchievement = new System.Windows.Forms.Label();
            this.btnCancel = new DarkUI.Controls.DarkButton();
            this.btnSave = new DarkUI.Controls.DarkButton();
            this.grpCompleteAchievement.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpCompleteAchievement
            // 
            this.grpCompleteAchievement.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.grpCompleteAchievement.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.grpCompleteAchievement.Controls.Add(this.cmbAchievements);
            this.grpCompleteAchievement.Controls.Add(this.lblAchievement);
            this.grpCompleteAchievement.Controls.Add(this.btnCancel);
            this.grpCompleteAchievement.Controls.Add(this.btnSave);
            this.grpCompleteAchievement.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpCompleteAchievement.Location = new System.Drawing.Point(3, 3);
            this.grpCompleteAchievement.Name = "grpCompleteAchievement";
            this.grpCompleteAchievement.Size = new System.Drawing.Size(200, 98);
            this.grpCompleteAchievement.TabIndex = 17;
            this.grpCompleteAchievement.TabStop = false;
            this.grpCompleteAchievement.Text = "Complete Achievement";
            // 
            // cmbAchievements
            // 
            this.cmbAchievements.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbAchievements.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbAchievements.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbAchievements.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbAchievements.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAchievements.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAchievements.FormattingEnabled = true;
            this.cmbAchievements.Location = new System.Drawing.Point(80, 19);
            this.cmbAchievements.Name = "cmbAchievements";
            this.cmbAchievements.Size = new System.Drawing.Size(112, 21);
            this.cmbAchievements.TabIndex = 22;
            // 
            // lblAchievement
            // 
            this.lblAchievement.AutoSize = true;
            this.lblAchievement.Location = new System.Drawing.Point(4, 22);
            this.lblAchievement.Name = "lblAchievement";
            this.lblAchievement.Size = new System.Drawing.Size(70, 13);
            this.lblAchievement.TabIndex = 21;
            this.lblAchievement.Text = "Achievement:";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(117, 69);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(5);
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(7, 69);
            this.btnSave.Name = "btnSave";
            this.btnSave.Padding = new System.Windows.Forms.Padding(5);
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 19;
            this.btnSave.Text = "Ok";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // EventCommandCompleteAchievement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.Controls.Add(this.grpCompleteAchievement);
            this.Name = "EventCommandCompleteAchievement";
            this.Size = new System.Drawing.Size(206, 106);
            this.grpCompleteAchievement.ResumeLayout(false);
            this.grpCompleteAchievement.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DarkGroupBox grpCompleteAchievement;
        private DarkButton btnCancel;
        private DarkButton btnSave;
        private System.Windows.Forms.Label lblAchievement;
        private DarkComboBox cmbAchievements;
    }
}
