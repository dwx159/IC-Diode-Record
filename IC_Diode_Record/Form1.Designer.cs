
namespace IC_Diode_Record
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            reflash_COM = new Button();
            row_num_label = new Label();
            col_num_label = new Label();
            row_set_textbox = new TextBox();
            col_set_textbox = new TextBox();
            dataGridView1 = new DataGridView();
            serialport_select = new ComboBox();
            open_serialport = new Button();
            write_btn = new Button();
            save_btn = new Button();
            rowcol_set_btn = new Button();
            enable_label = new Label();
            cellset_groupBox = new GroupBox();
            rowcol_set_label = new Label();
            clear_data_label = new Label();
            switchvertical_label = new Label();
            direction_comboBox = new ComboBox();
            clear_data_btn = new Button();
            inportExcel_btn = new Button();
            serialpoart_groupBox = new GroupBox();
            data_groupBox = new GroupBox();
            inportCompare_label = new Label();
            inportExcel_label = new Label();
            inportCompare_btn = new Button();
            train_groupBox = new GroupBox();
            train_label = new Label();
            train_btn = new Button();
            train_textbox = new TextBox();
            panel1 = new Panel();
            panel2 = new Panel();
            help_groupBox = new GroupBox();
            help_btn = new Button();
            voice_groupBox = new GroupBox();
            voicestate_RioBtn = new RadioButton();
            voiceAzure_btn = new Button();
            voiceControl_btn = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            cellset_groupBox.SuspendLayout();
            serialpoart_groupBox.SuspendLayout();
            data_groupBox.SuspendLayout();
            train_groupBox.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            help_groupBox.SuspendLayout();
            voice_groupBox.SuspendLayout();
            SuspendLayout();
            // 
            // reflash_COM
            // 
            reflash_COM.Location = new Point(7, 24);
            reflash_COM.Margin = new Padding(4);
            reflash_COM.Name = "reflash_COM";
            reflash_COM.Size = new Size(122, 35);
            reflash_COM.TabIndex = 0;
            reflash_COM.Text = "刷新串口";
            reflash_COM.UseVisualStyleBackColor = true;
            reflash_COM.Click += reflash_COM_Click;
            // 
            // row_num_label
            // 
            row_num_label.AutoSize = true;
            row_num_label.Location = new Point(12, 32);
            row_num_label.Margin = new Padding(4, 0, 4, 0);
            row_num_label.Name = "row_num_label";
            row_num_label.Size = new Size(46, 24);
            row_num_label.TabIndex = 1;
            row_num_label.Text = "行数";
            // 
            // col_num_label
            // 
            col_num_label.AutoSize = true;
            col_num_label.Location = new Point(12, 64);
            col_num_label.Margin = new Padding(4, 0, 4, 0);
            col_num_label.Name = "col_num_label";
            col_num_label.Size = new Size(46, 24);
            col_num_label.TabIndex = 2;
            col_num_label.Text = "列数";
            // 
            // row_set_textbox
            // 
            row_set_textbox.Location = new Point(73, 28);
            row_set_textbox.Margin = new Padding(4);
            row_set_textbox.Name = "row_set_textbox";
            row_set_textbox.Size = new Size(60, 30);
            row_set_textbox.TabIndex = 3;
            row_set_textbox.Text = "20";
            row_set_textbox.KeyPress += onlyNum_KeyPress;
            // 
            // col_set_textbox
            // 
            col_set_textbox.Location = new Point(73, 61);
            col_set_textbox.Margin = new Padding(4);
            col_set_textbox.Name = "col_set_textbox";
            col_set_textbox.Size = new Size(60, 30);
            col_set_textbox.TabIndex = 4;
            col_set_textbox.Text = "20";
            col_set_textbox.KeyPress += onlyNum_KeyPress;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(5, 122);
            dataGridView1.Margin = new Padding(4);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.RowTemplate.Height = 30;
            dataGridView1.Size = new Size(1638, 600);
            dataGridView1.TabIndex = 5;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.KeyDown += dataGridView1_KeyDown;
            // 
            // serialport_select
            // 
            serialport_select.FormattingEnabled = true;
            serialport_select.Location = new Point(7, 63);
            serialport_select.Margin = new Padding(4);
            serialport_select.Name = "serialport_select";
            serialport_select.Size = new Size(369, 32);
            serialport_select.TabIndex = 6;
            // 
            // open_serialport
            // 
            open_serialport.BackColor = Color.LightGreen;
            open_serialport.Location = new Point(254, 24);
            open_serialport.Margin = new Padding(4);
            open_serialport.Name = "open_serialport";
            open_serialport.Size = new Size(122, 35);
            open_serialport.TabIndex = 7;
            open_serialport.Text = "连接设备";
            open_serialport.UseVisualStyleBackColor = false;
            open_serialport.Click += open_serialport_Click;
            // 
            // write_btn
            // 
            write_btn.BackColor = Color.Chocolate;
            write_btn.Location = new Point(8, 26);
            write_btn.Margin = new Padding(4);
            write_btn.Name = "write_btn";
            write_btn.Size = new Size(100, 70);
            write_btn.TabIndex = 8;
            write_btn.Text = "写入";
            write_btn.UseVisualStyleBackColor = false;
            write_btn.Click += write_btn_Click;
            // 
            // save_btn
            // 
            save_btn.Location = new Point(260, 28);
            save_btn.Margin = new Padding(4);
            save_btn.Name = "save_btn";
            save_btn.Size = new Size(65, 68);
            save_btn.TabIndex = 9;
            save_btn.Text = "保存";
            save_btn.UseVisualStyleBackColor = true;
            save_btn.Click += save_btn_Click;
            // 
            // rowcol_set_btn
            // 
            rowcol_set_btn.Location = new Point(141, 53);
            rowcol_set_btn.Margin = new Padding(4);
            rowcol_set_btn.Name = "rowcol_set_btn";
            rowcol_set_btn.Size = new Size(67, 41);
            rowcol_set_btn.TabIndex = 10;
            rowcol_set_btn.Text = "设置";
            rowcol_set_btn.UseVisualStyleBackColor = true;
            rowcol_set_btn.Click += rowcol_set_btn_Click;
            // 
            // enable_label
            // 
            enable_label.AutoSize = true;
            enable_label.ForeColor = Color.SeaGreen;
            enable_label.Location = new Point(94, 0);
            enable_label.Margin = new Padding(4, 0, 4, 0);
            enable_label.Name = "enable_label";
            enable_label.Size = new Size(158, 24);
            enable_label.TabIndex = 11;
            enable_label.Text = "Ctrl+D禁用单元格";
            // 
            // cellset_groupBox
            // 
            cellset_groupBox.Controls.Add(rowcol_set_label);
            cellset_groupBox.Controls.Add(clear_data_label);
            cellset_groupBox.Controls.Add(switchvertical_label);
            cellset_groupBox.Controls.Add(direction_comboBox);
            cellset_groupBox.Controls.Add(clear_data_btn);
            cellset_groupBox.Controls.Add(rowcol_set_btn);
            cellset_groupBox.Controls.Add(enable_label);
            cellset_groupBox.Controls.Add(row_num_label);
            cellset_groupBox.Controls.Add(col_num_label);
            cellset_groupBox.Controls.Add(row_set_textbox);
            cellset_groupBox.Controls.Add(col_set_textbox);
            cellset_groupBox.Location = new Point(5, 4);
            cellset_groupBox.Margin = new Padding(4);
            cellset_groupBox.Name = "cellset_groupBox";
            cellset_groupBox.Padding = new Padding(4);
            cellset_groupBox.Size = new Size(364, 104);
            cellset_groupBox.TabIndex = 12;
            cellset_groupBox.TabStop = false;
            cellset_groupBox.Text = "表格设置";
            // 
            // rowcol_set_label
            // 
            rowcol_set_label.AutoSize = true;
            rowcol_set_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            rowcol_set_label.Location = new Point(140, 28);
            rowcol_set_label.Name = "rowcol_set_label";
            rowcol_set_label.Size = new Size(69, 20);
            rowcol_set_label.TabIndex = 18;
            rowcol_set_label.Text = "生成表格";
            // 
            // clear_data_label
            // 
            clear_data_label.AutoSize = true;
            clear_data_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            clear_data_label.Location = new Point(215, 28);
            clear_data_label.Name = "clear_data_label";
            clear_data_label.Size = new Size(69, 20);
            clear_data_label.TabIndex = 15;
            clear_data_label.Text = "清除数据";
            // 
            // switchvertical_label
            // 
            switchvertical_label.AutoSize = true;
            switchvertical_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            switchvertical_label.Location = new Point(290, 28);
            switchvertical_label.Name = "switchvertical_label";
            switchvertical_label.Size = new Size(69, 20);
            switchvertical_label.TabIndex = 14;
            switchvertical_label.Text = "切换方向";
            // 
            // direction_comboBox
            // 
            direction_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            direction_comboBox.FormattingEnabled = true;
            direction_comboBox.Items.AddRange(new object[] { "横向", "竖向", "逆时针" });
            direction_comboBox.Location = new Point(290, 57);
            direction_comboBox.Name = "direction_comboBox";
            direction_comboBox.Size = new Size(67, 32);
            direction_comboBox.TabIndex = 13;
            direction_comboBox.SelectedIndexChanged += direction_comboBox_SelectedIndexChanged;
            // 
            // clear_data_btn
            // 
            clear_data_btn.Location = new Point(216, 53);
            clear_data_btn.Margin = new Padding(4);
            clear_data_btn.Name = "clear_data_btn";
            clear_data_btn.Size = new Size(67, 41);
            clear_data_btn.TabIndex = 12;
            clear_data_btn.Text = "清除";
            clear_data_btn.UseVisualStyleBackColor = true;
            clear_data_btn.Click += clear_data_btn_Click;
            // 
            // inportExcel_btn
            // 
            inportExcel_btn.Location = new Point(116, 53);
            inportExcel_btn.Margin = new Padding(4);
            inportExcel_btn.Name = "inportExcel_btn";
            inportExcel_btn.Size = new Size(65, 43);
            inportExcel_btn.TabIndex = 13;
            inportExcel_btn.Text = "导入";
            inportExcel_btn.UseVisualStyleBackColor = true;
            inportExcel_btn.Click += inportExcel_btn_Click;
            // 
            // serialpoart_groupBox
            // 
            serialpoart_groupBox.Controls.Add(serialport_select);
            serialpoart_groupBox.Controls.Add(reflash_COM);
            serialpoart_groupBox.Controls.Add(open_serialport);
            serialpoart_groupBox.Location = new Point(377, 4);
            serialpoart_groupBox.Margin = new Padding(4);
            serialpoart_groupBox.Name = "serialpoart_groupBox";
            serialpoart_groupBox.Padding = new Padding(4);
            serialpoart_groupBox.Size = new Size(383, 104);
            serialpoart_groupBox.TabIndex = 13;
            serialpoart_groupBox.TabStop = false;
            serialpoart_groupBox.Text = "串口设置";
            // 
            // data_groupBox
            // 
            data_groupBox.Controls.Add(inportCompare_label);
            data_groupBox.Controls.Add(inportExcel_label);
            data_groupBox.Controls.Add(inportCompare_btn);
            data_groupBox.Controls.Add(inportExcel_btn);
            data_groupBox.Controls.Add(write_btn);
            data_groupBox.Controls.Add(save_btn);
            data_groupBox.Location = new Point(768, 4);
            data_groupBox.Margin = new Padding(4);
            data_groupBox.Name = "data_groupBox";
            data_groupBox.Padding = new Padding(4);
            data_groupBox.Size = new Size(337, 104);
            data_groupBox.TabIndex = 14;
            data_groupBox.TabStop = false;
            data_groupBox.Text = "数据操作";
            // 
            // inportCompare_label
            // 
            inportCompare_label.AutoSize = true;
            inportCompare_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            inportCompare_label.Location = new Point(184, 28);
            inportCompare_label.Name = "inportCompare_label";
            inportCompare_label.Size = new Size(69, 20);
            inportCompare_label.TabIndex = 20;
            inportCompare_label.Text = "对比数据";
            // 
            // inportExcel_label
            // 
            inportExcel_label.AutoSize = true;
            inportExcel_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            inportExcel_label.Location = new Point(113, 28);
            inportExcel_label.Name = "inportExcel_label";
            inportExcel_label.Size = new Size(69, 20);
            inportExcel_label.TabIndex = 19;
            inportExcel_label.Text = "导入数据";
            // 
            // inportCompare_btn
            // 
            inportCompare_btn.Location = new Point(188, 53);
            inportCompare_btn.Name = "inportCompare_btn";
            inportCompare_btn.Size = new Size(65, 43);
            inportCompare_btn.TabIndex = 14;
            inportCompare_btn.Text = "对比";
            inportCompare_btn.UseVisualStyleBackColor = true;
            inportCompare_btn.Click += inportCompare_btn_Click;
            // 
            // train_groupBox
            // 
            train_groupBox.Controls.Add(train_label);
            train_groupBox.Controls.Add(train_btn);
            train_groupBox.Controls.Add(train_textbox);
            train_groupBox.Location = new Point(1113, 4);
            train_groupBox.Margin = new Padding(4);
            train_groupBox.Name = "train_groupBox";
            train_groupBox.Padding = new Padding(4);
            train_groupBox.Size = new Size(253, 104);
            train_groupBox.TabIndex = 15;
            train_groupBox.TabStop = false;
            train_groupBox.Text = "校准设置";
            // 
            // train_label
            // 
            train_label.AutoSize = true;
            train_label.Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            train_label.ForeColor = Color.Maroon;
            train_label.Location = new Point(84, 35);
            train_label.Margin = new Padding(4, 0, 4, 0);
            train_label.Name = "train_label";
            train_label.Size = new Size(167, 60);
            train_label.TabIndex = 2;
            train_label.Text = "首次连接后校准\r\n1.测试一个值然后输入\r\n2.再点击校准(边侧边点)";
            // 
            // train_btn
            // 
            train_btn.Location = new Point(6, 62);
            train_btn.Margin = new Padding(4);
            train_btn.Name = "train_btn";
            train_btn.Size = new Size(70, 35);
            train_btn.TabIndex = 1;
            train_btn.Text = "校准";
            train_btn.UseVisualStyleBackColor = true;
            train_btn.Click += train_btn_Click;
            // 
            // train_textbox
            // 
            train_textbox.Location = new Point(6, 24);
            train_textbox.Margin = new Padding(4);
            train_textbox.Name = "train_textbox";
            train_textbox.Size = new Size(70, 30);
            train_textbox.TabIndex = 0;
            train_textbox.KeyPress += train_textbox_KeyPress;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(dataGridView1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1654, 734);
            panel1.TabIndex = 16;
            // 
            // panel2
            // 
            panel2.Controls.Add(help_groupBox);
            panel2.Controls.Add(voice_groupBox);
            panel2.Controls.Add(cellset_groupBox);
            panel2.Controls.Add(data_groupBox);
            panel2.Controls.Add(train_groupBox);
            panel2.Controls.Add(serialpoart_groupBox);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1654, 115);
            panel2.TabIndex = 17;
            // 
            // help_groupBox
            // 
            help_groupBox.Controls.Add(help_btn);
            help_groupBox.Location = new Point(1512, 4);
            help_groupBox.Name = "help_groupBox";
            help_groupBox.Size = new Size(130, 104);
            help_groupBox.TabIndex = 17;
            help_groupBox.TabStop = false;
            help_groupBox.Text = "使用说明";
            // 
            // help_btn
            // 
            help_btn.Location = new Point(34, 26);
            help_btn.Margin = new Padding(4);
            help_btn.Name = "help_btn";
            help_btn.Size = new Size(65, 68);
            help_btn.TabIndex = 21;
            help_btn.Text = "说明";
            help_btn.UseVisualStyleBackColor = true;
            help_btn.Click += help_btn_Click;
            // 
            // voice_groupBox
            // 
            voice_groupBox.Controls.Add(voicestate_RioBtn);
            voice_groupBox.Controls.Add(voiceAzure_btn);
            voice_groupBox.Controls.Add(voiceControl_btn);
            voice_groupBox.Location = new Point(1373, 4);
            voice_groupBox.Name = "voice_groupBox";
            voice_groupBox.Size = new Size(132, 104);
            voice_groupBox.TabIndex = 16;
            voice_groupBox.TabStop = false;
            voice_groupBox.Text = "语音控制";
            // 
            // voicestate_RioBtn
            // 
            voicestate_RioBtn.AutoSize = true;
            voicestate_RioBtn.Enabled = false;
            voicestate_RioBtn.Location = new Point(24, 62);
            voicestate_RioBtn.Name = "voicestate_RioBtn";
            voicestate_RioBtn.Size = new Size(89, 28);
            voicestate_RioBtn.TabIndex = 6;
            voicestate_RioBtn.TabStop = true;
            voicestate_RioBtn.Text = "已关闭";
            voicestate_RioBtn.UseVisualStyleBackColor = true;
            // 
            // voiceAzure_btn
            // 
            voiceAzure_btn.Location = new Point(68, 29);
            voiceAzure_btn.Name = "voiceAzure_btn";
            voiceAzure_btn.Size = new Size(58, 32);
            voiceAzure_btn.TabIndex = 17;
            voiceAzure_btn.Text = "密钥";
            voiceAzure_btn.UseVisualStyleBackColor = true;
            voiceAzure_btn.Click += voiceAzure_btn_Click;
            // 
            // voiceControl_btn
            // 
            voiceControl_btn.Location = new Point(6, 28);
            voiceControl_btn.Name = "voiceControl_btn";
            voiceControl_btn.Size = new Size(58, 32);
            voiceControl_btn.TabIndex = 16;
            voiceControl_btn.Text = "语音";
            voiceControl_btn.UseVisualStyleBackColor = true;
            voiceControl_btn.Click += voiceControl_btn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1654, 734);
            Controls.Add(panel2);
            Controls.Add(panel1);
            KeyPreview = true;
            Margin = new Padding(4);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            KeyDown += Form1_KeyDown;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            cellset_groupBox.ResumeLayout(false);
            cellset_groupBox.PerformLayout();
            serialpoart_groupBox.ResumeLayout(false);
            data_groupBox.ResumeLayout(false);
            data_groupBox.PerformLayout();
            train_groupBox.ResumeLayout(false);
            train_groupBox.PerformLayout();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            help_groupBox.ResumeLayout(false);
            voice_groupBox.ResumeLayout(false);
            voice_groupBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button reflash_COM;
        private System.Windows.Forms.Label row_num_label;
        private System.Windows.Forms.Label col_num_label;
        private System.Windows.Forms.TextBox row_set_textbox;
        private System.Windows.Forms.TextBox col_set_textbox;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox serialport_select;
        private System.Windows.Forms.Button open_serialport;
        private System.Windows.Forms.Button write_btn;
        private System.Windows.Forms.Button save_btn;
        private System.Windows.Forms.Button rowcol_set_btn;
        private System.Windows.Forms.Label enable_label;
        private System.Windows.Forms.GroupBox cellset_groupBox;
        private System.Windows.Forms.GroupBox serialpoart_groupBox;
        private System.Windows.Forms.GroupBox data_groupBox;
        private System.Windows.Forms.GroupBox train_groupBox;
        private System.Windows.Forms.Button train_btn;
        private System.Windows.Forms.TextBox train_textbox;
        private System.Windows.Forms.Label train_label;
        private System.Windows.Forms.Button clear_data_btn;
        private System.Windows.Forms.Button inportExcel_btn;
        private Panel panel1;
        private Panel panel2;
        private Button voiceControl_btn;
        private Button voiceAzure_btn;
        private GroupBox voice_groupBox;
        private RadioButton voicestate_RioBtn;
        private Button inportCompare_btn;
        private ComboBox direction_comboBox;
        private Label switchvertical_label;
        private Label clear_data_label;
        private Label rowcol_set_label;
        private Label inportCompare_label;
        private Label inportExcel_label;
        private GroupBox help_groupBox;
        private Button help_btn;
    }
}

