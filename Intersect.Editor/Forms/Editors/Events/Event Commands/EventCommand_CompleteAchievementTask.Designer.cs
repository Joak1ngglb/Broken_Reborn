using DarkUI.Controls;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands
{
    partial class EventCommandCompleteAchievementTask
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
            this.cmbAchievementTask = new DarkUI.Controls.DarkComboBox();
            this.lblTask = new System.Windows.Forms.Label();
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
            this.grpCompleteAchievement.Controls.Add(this.cmbAchievementTask);
            this.grpCompleteAchievement.Controls.Add(this.lblTask);
            this.grpCompleteAchievement.Controls.Add(this.cmbAchievements);
            this.grpCompleteAchievement.Controls.Add(this.lblAchievement);
            this.grpCompleteAchievement.Controls.Add(this.btnCancel);
            this.grpCompleteAchievement.Controls.Add(this.btnSave);
            this.grpCompleteAchievement.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpCompleteAchievement.Location = new System.Drawing.Point(3, 3);
            this.grpCompleteAchievement.Name = "grpCompleteAchievement";
            this.grpCompleteAchievement.Size = new System.Drawing.Size(216, 126);
            this.grpCompleteAchievement.TabIndex = 17;
            this.grpCompleteAchievement.TabStop = false;
            this.grpCompleteAchievement.Text = "Complete Achievement Task";
            // 
            // cmbAchievementTask
            // 
            this.cmbAchievementTask.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbAchievementTask.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbAchievementTask.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbAchievementTask.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbAchievementTask.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAchievementTask.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAchievementTask.FormattingEnabled = true;
            this.cmbAchievementTask.Location = new System.Drawing.Point(80, 47);
            this.cmbAchievementTask.Name = "cmbAchievementTask";
            this.cmbAchievementTask.Size = new System.Drawing.Size(127, 21);
            this.cmbAchievementTask.TabIndex = 24;
            // 
            // lblTask
            // 
            this.lblTask.AutoSize = true;
            this.lblTask.Location = new System.Drawing.Point(4, 50);
            this.lblTask.Name = "lblTask";
            this.lblTask.Size = new System.Drawing.Size(34, 13);
            this.lblTask.TabIndex = 23;
            this.lblTask.Text = "Task:";
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
            this.cmbAchievements.Size = new System.Drawing.Size(127, 21);
            this.cmbAchievements.TabIndex = 22;
            this.cmbAchievements.SelectedIndexChanged += new System.EventHandler(this.cmbAchievements_SelectedIndexChanged);
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
            this.btnCancel.Location = new System.Drawing.Point(132, 97);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(5);
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(7, 97);
            this.btnSave.Name = "btnSave";
            this.btnSave.Padding = new System.Windows.Forms.Padding(5);
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 19;
            this.btnSave.Text = "Ok";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // EventCommandCompleteAchievementTask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.Controls.Add(this.grpCompleteAchievement);
            this.Name = "EventCommandCompleteAchievementTask";
            this.Size = new System.Drawing.Size(222, 132);
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
        private DarkComboBox cmbAchievementTask;
        private System.Windows.Forms.Label lblTask;
    }
}
