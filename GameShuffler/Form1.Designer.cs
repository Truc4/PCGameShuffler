using System.Runtime.InteropServices;

namespace GameShuffler
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            if (disposing)
            {
                UnregisterHotKey(this.Handle, RemoveGameKeyId);
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            runningProcessesSelectionList = new DataGridView();
            desiredGamesList = new DataGridView();
            label1 = new Label();
            label2 = new Label();
            startButton = new Button();
            label3 = new Label();
            minTimeTextBox = new TextBox();
            maxTimeTextBox = new TextBox();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            refreshButton = new Button();
            stopButton = new Button();
            label7 = new Label();
            label8 = new Label();
            SuspendLayout();
            // 
            // runningProcessesSelectionList
            // 
            runningProcessesSelectionList.AllowUserToAddRows = false;
            runningProcessesSelectionList.AllowUserToDeleteRows = false;
            runningProcessesSelectionList.AllowUserToResizeColumns = false;
            runningProcessesSelectionList.AllowUserToResizeRows = false;
            runningProcessesSelectionList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            runningProcessesSelectionList.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Select", Width = 50 });
            runningProcessesSelectionList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Window Title", Width = 250 });
            runningProcessesSelectionList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Process ID", Visible = false });
            runningProcessesSelectionList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Process Name", Width = 250 });
            runningProcessesSelectionList.Columns.Add(new DataGridViewButtonColumn() { HeaderText = "Attach", Text = "Attach", UseColumnTextForButtonValue = true, Width = 70 });
            runningProcessesSelectionList.Location = new Point(40, 40);
            runningProcessesSelectionList.Name = "runningProcessesSelectionList";
            runningProcessesSelectionList.Size = new Size(500, 400);
            runningProcessesSelectionList.TabIndex = 0;
            runningProcessesSelectionList.CellContentClick += RunningProcessesSelectionList_CellContentClick;
            runningProcessesSelectionList.CellValueChanged += RunningProcessesSelectionList_CellValueChanged;
            runningProcessesSelectionList.CurrentCellDirtyStateChanged += RunningProcessesSelectionList_CurrentCellDirtyStateChanged;
            // 
            // desiredGamesList
            // 
            desiredGamesList.AllowUserToAddRows = false;
            desiredGamesList.AllowUserToDeleteRows = false;
            desiredGamesList.AllowUserToResizeColumns = false;
            desiredGamesList.AllowUserToResizeRows = false;
            desiredGamesList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            desiredGamesList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Window Title", Width = 200 });
            desiredGamesList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Process Name", Width = 200 });
            desiredGamesList.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Pause", Width = 50, TrueValue = true, FalseValue = false, DefaultCellStyle = new DataGridViewCellStyle { NullValue = true } });
            desiredGamesList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Attached Game", Width = 200 });
            desiredGamesList.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Fullscreen", Width = 50, TrueValue = true, FalseValue = false, DefaultCellStyle = new DataGridViewCellStyle { NullValue = true } });
            desiredGamesList.Columns.Add(new DataGridViewButtonColumn() { HeaderText = "Remove", Text = "X", UseColumnTextForButtonValue = true, Width = 50 });
            desiredGamesList.Location = new Point(560, 40);
            desiredGamesList.Name = "desiredGamesList";
            desiredGamesList.Size = new Size(600, 400);
            desiredGamesList.TabIndex = 1;
            desiredGamesList.CellContentClick += DesiredGamesList_CellContentClick;
            desiredGamesList.CellValueChanged += DesiredGamesList_CellValueChanged;
            desiredGamesList.CurrentCellDirtyStateChanged += DesiredGamesList_CurrentCellDirtyStateChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(40, 9);
            label1.Name = "label1";
            label1.Size = new Size(200, 25);
            label1.TabIndex = 2;
            label1.Text = "Running processes:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(560, 9);
            label2.Name = "label2";
            label2.Size = new Size(200, 25);
            label2.TabIndex = 3;
            label2.Text = "Desired games to shuffle:";
            // 
            // startButton
            // 
            startButton.Location = new Point(700, 500);
            startButton.Name = "startButton";
            startButton.Size = new Size(133, 34);
            startButton.TabIndex = 4;
            startButton.Text = "Start Shuffling";
            startButton.UseVisualStyleBackColor = true;
            startButton.Click += StartButton_Clicked;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(40, 460);
            label3.Name = "label3";
            label3.Size = new Size(263, 25);
            label3.TabIndex = 5;
            label3.Text = "Minimum time before shuffling:";
            // 
            // minTimeTextBox
            // 
            minTimeTextBox.Location = new Point(309, 457);
            minTimeTextBox.Name = "minTimeTextBox";
            minTimeTextBox.Size = new Size(150, 31);
            minTimeTextBox.TabIndex = 6;
            minTimeTextBox.Text = "60";
            // 
            // maxTimeTextBox
            // 
            maxTimeTextBox.Location = new Point(309, 494);
            maxTimeTextBox.Name = "maxTimeTextBox";
            maxTimeTextBox.Size = new Size(150, 31);
            maxTimeTextBox.TabIndex = 7;
            maxTimeTextBox.Text = "120";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(40, 497);
            label4.Name = "label4";
            label4.Size = new Size(266, 25);
            label4.TabIndex = 8;
            label4.Text = "Maximum time before shuffling:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(465, 460);
            label5.Name = "label5";
            label5.Size = new Size(77, 25);
            label5.TabIndex = 9;
            label5.Text = "seconds";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(465, 497);
            label6.Name = "label6";
            label6.Size = new Size(77, 25);
            label6.TabIndex = 10;
            label6.Text = "seconds";
            // 
            // refreshButton
            // 
            refreshButton.Location = new Point(428, 4);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(112, 34);
            refreshButton.TabIndex = 11;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += Refresh_Clicked;
            // 
            // stopButton
            // 
            stopButton.Enabled = false;
            stopButton.Location = new Point(839, 500);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(149, 34);
            stopButton.TabIndex = 12;
            stopButton.Text = "Stop Shuffling";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += StopButton_Clicked;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(40, 540);
            label7.Name = "label7";
            label7.Size = new Size(331, 25);
            label7.TabIndex = 13;
            label7.Text = "Press PageUp to move to the next game";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(40, 570);
            label8.Name = "label8";
            label8.Size = new Size(331, 25);
            label8.TabIndex = 14;
            label8.Text = "Press PageDown to remove the current game";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 600);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(stopButton);
            Controls.Add(refreshButton);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(maxTimeTextBox);
            Controls.Add(minTimeTextBox);
            Controls.Add(label3);
            Controls.Add(startButton);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(desiredGamesList);
            Controls.Add(runningProcessesSelectionList);
            Name = "Form1";
            Text = "Game Shuffler";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView runningProcessesSelectionList;
        private DataGridView desiredGamesList;
        private Label label1;
        private Label label2;
        private Button startButton;
        private Label label3;
        private TextBox minTimeTextBox;
        private TextBox maxTimeTextBox;
        private Label label4;
        private Label label5;
        private Label label6;
        private Button refreshButton;
        private Button stopButton;
        private Label label7;
        private Label label8;
    }
}