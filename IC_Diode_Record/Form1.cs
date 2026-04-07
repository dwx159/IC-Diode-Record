using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using OfficeOpenXml;
using System.IO;
using System.IO.Ports;
using System.Speech.Recognition;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;


namespace IC_Diode_Record
{
    public partial class Form1 : Form
    {
        private SerialPort serialPort = new SerialPort();


        public Form1()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.None;        //Form 的 AutoScaleMode 设置为 None

        }




        //Form1_Load函数
        private void Form1_Load(object sender, EventArgs e)
        {
            // 初始化 DataGridView 右键菜单
            ContextMenuStrip cms = new ContextMenuStrip();
            // 复制
            ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
            copyItem.Click += (s, e2) =>
            {
                CopySelectedCells(); // 调用你写好的方法
            };
            cms.Items.Add(copyItem);
            // 粘贴
            ToolStripMenuItem pasteItem = new ToolStripMenuItem("粘贴");
            pasteItem.Click += (s, e2) =>
            {
                PasteToSelectedCell(); // 调用你写好的方法
            };
            cms.Items.Add(pasteItem);
            // 绑定到 DataGridView
            dataGridView1.ContextMenuStrip = cms;


            //// ===== 防止自动滚动用 =====
            dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            dataGridView1.CurrentCellChanged += dataGridView1_CurrentCellChanged;

        }



        #region keypress函数。限制textbox只能输入数字、小数
        private void onlyNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;

            // 允许退格键
            if (char.IsControl(e.KeyChar))
                return;

            // 允许数字
            if (char.IsDigit(e.KeyChar))
                return;

            // 其他字符都阻止
            e.Handled = true;
        }

        // 校准的textbox，只允许输入数字、小数
        private void train_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;

            // 允许退格键
            if (char.IsControl(e.KeyChar))
                return;

            // 允许数字
            if (char.IsDigit(e.KeyChar))
                return;

            // 允许小数点，但只能有一个
            if (e.KeyChar == '.' && !tb.Text.Contains("."))
                return;

            // 其他字符都阻止
            e.Handled = true;
        }
        #endregion



        #region 重置datagridview函数
        // 重置datagridview
        private void ResetDataGridView()
        {
            dataGridView1.SuspendLayout();

            // 1. 取消数据源（如果以后你用了 DataSource）
            dataGridView1.DataSource = null;

            // 2. 清空结构
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            // 3. 清空选择
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;

            // 4. 重置样式
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.Font = new Font("微软雅黑", 6);

            dataGridView1.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;

            // 5. 重置行为
            dataGridView1.ReadOnly = false;
            dataGridView1.MultiSelect = true;
            dataGridView1.AllowUserToAddRows = false;

            dataGridView1.ResumeLayout();
        }
        #endregion



        #region 创建datagridview按键点击函数
        //设置DataGridView点击事件
        private void rowcol_set_btn_Click(object sender, EventArgs e)
        {
            // 弹出提示框
            DialogResult result = MessageBox.Show(
                "重置表格会清空数据？",
                "提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                ResetDataGridView();   // 先重置

                //从输入框中取行列大小
                int row = Convert.ToInt32(row_set_textbox.Text);
                int col = Convert.ToInt32(col_set_textbox.Text);

                datagridview_creat(row, col);

                // 重置记录位置
                currentRow = 0;
                currentCol = 0;
            }
        }
        #endregion



        #region 创建datagridview函数
        //创建datagridview大小和格式
        private void datagridview_creat(int row, int col)
        {
            // 禁止自动调整尺寸
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            // 行标题数组索引
            string[] row_name = { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "R", "T", "U", "V", "W", "Y",
                 "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AJ", "AK", "AL", "AM", "AN", "AP", "AR", "AT", "AU", "AV", "AW", "AY"};

            //设置datagridview的大小
            dataGridView1.ColumnCount = col;
            dataGridView1.RowCount = row;

            // 设置列标题和列宽
            for (int i = 0; i < col; i++)
            {
                dataGridView1.Columns[i].HeaderText = $"{i + 1}";
                dataGridView1.Columns[i].Width = 50;

                dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable; // 禁用点击列标题自动排序
            }

            // 设置行标题和行高
            for (int i = 0; i < row; i++)
            {
                dataGridView1.Rows[i].HeaderCell.Value = $"{row_name[i]}";
                dataGridView1.Rows[i].Height = 35;
            }

            // 设置行/列标题字体大小为 6
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 6, FontStyle.Bold);
            dataGridView1.RowHeadersDefaultCellStyle.Font = new Font("微软雅黑", 6, FontStyle.Regular);
            // 设置数据区域字体大小为6
            dataGridView1.DefaultCellStyle.Font = new Font("微软雅黑", 6); // 6号字

            // Ctrl+C / Ctrl+V 复制粘贴，datagridview的设置
            dataGridView1.MultiSelect = true;                 // 允许多选
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;     //
            dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable; //DataGridView 内置的 ClipboardCopyMode.因为它对 ReadOnly / 自定义逻辑不好控制


            ResizeFormToFitGrid();  //根据单元格计算调整窗口大小
        }

        // 根据单元格计算窗口大小
        private void ResizeFormToFitGrid()
        {
            int totalWidth = dataGridView1.RowHeadersWidth;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
                totalWidth += col.Width;

            int totalHeight = dataGridView1.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dataGridView1.Rows)
                totalHeight += row.Height;

            // 20的边距
            totalWidth += 20;
            totalHeight += 20;
            //限制datagridview最小宽度和高度
            if (totalWidth < 1625)
            {
                totalWidth = 1625;
            }
            if (totalHeight < 600)
            {
                totalHeight = 600;
            }
            dataGridView1.Width = totalWidth;
            dataGridView1.Height = totalHeight;

            //限制窗口的最大宽度和高度
            int form_width = totalWidth + 20;       //程序窗口要比datagridview宽20
            int form_height = totalHeight + 135;  //程序窗口要比datagridview高135
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            if (form_width > workArea.Width)
            {
                form_width = workArea.Width;
            }
            if (form_height > workArea.Height)
            {
                form_height = workArea.Height - 80; //再往下降一点，避免填满
            }
            this.ClientSize = new Size(form_width, form_height);
        }
        #endregion



        #region 防止滚动条自动滚动
        // 防止滚动条自动滚动
        // 全局变量，只记录编辑时滚动
        private Point _panelScrollPos;
        private bool _isEditing = false;
        // 开始编辑单元格
        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            _isEditing = true;
            _panelScrollPos = panel1.AutoScrollPosition;
        }
        // 结束编辑单元格
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            _isEditing = false;

            // 仅在编辑时恢复滚动
            panel1.AutoScrollPosition = new Point(-_panelScrollPos.X, -_panelScrollPos.Y);
        }
        // 普通选择单元格时不强制回滚
        // （可选，如果你之前在 CurrentCellChanged 里回滚，可以删除）
        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (_isEditing)
            {
                // 编辑过程中保持滚动
                panel1.AutoScrollPosition = new Point(-_panelScrollPos.X, -_panelScrollPos.Y);
            }
        }
        #endregion



        #region datagridview按键函数（Ctrl+D禁用/启用单元格、Ctrl+C / Ctrl+V（复制/粘贴多个单元格））
        // datagridview按键函数
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+D：对【所有选中单元格】切换禁用/启用
            if (e.Control && e.KeyCode == Keys.D)
            {
                ToggleSelectedCellsReadOnly();
                e.Handled = true;
                return;
            }

            // Ctrl+C / Ctrl+V（复制/粘贴多个单元格）
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedCells(); // 调用你写好的方法
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteToSelectedCell(); // 调用你写好的方法
                e.Handled = true;
            }

        }
        #endregion



        #region 多选禁用单元格函数
        // 多选禁用单元格
        private void ToggleSelectedCellsReadOnly()
        {
            if (dataGridView1.SelectedCells.Count == 0)
                return;

            // 判断：只要有一个是可写的 → 统一“禁用”
            bool shouldDisable = dataGridView1.SelectedCells
                .Cast<DataGridViewCell>()
                .Any(c => !c.ReadOnly);

            foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
            {
                if (cell.RowIndex < 0 || cell.ColumnIndex < 0)
                    continue;

                if (shouldDisable)
                {
                    // 禁用
                    cell.ReadOnly = true;
                    cell.Style.BackColor = Color.LightGray;
                    cell.Tag = "DISABLED";
                }
                else
                {
                    // 启用
                    cell.ReadOnly = false;
                    cell.Style.BackColor = Color.White;
                    cell.Tag = null;
                }
            }
        }
        #endregion



        #region 清除按键点击事件---清除单元格数据，不清除禁用结构
        //清除按键点击事件---清除单元格数据，不清除禁用结构
        private void clear_data_btn_Click(object sender, EventArgs e)
        {
            // 弹出提示框
            DialogResult result = MessageBox.Show(
                "确定清除数据？（禁用区域不影响）",
                "提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 清除数据
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Value = null;
                    }
                }
            }

        }
        #endregion



        #region 导入数据按键函数（禁用、数值、颜色、tag、）
        //导入按键点击事件
        private void inportExcel_btn_Click(object sender, EventArgs e)
        {
            //打开对话框，选择excel模板文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel文件|*.xlsx;*.xls";
            openFileDialog.Title = "选择Excel文件";


            string filePath = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                //获取excel模板路劲
                filePath = openFileDialog.FileName;


                //重置datagridview
                ResetDataGridView();

                // 重置记录位置
                currentRow = 0;
                currentCol = 0;


                try
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(filePath)))
                    {
                        // 获取第一个工作表
                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
                        //excel没有数据就提示错误
                        if (worksheet.Dimension == null)
                        {
                            MessageBox.Show("Excel 模板中没有数据");
                            return;
                        }

                        // 获取行数和列数
                        int rowCount_excel = worksheet.Dimension.Rows;      // excel总行数
                        int colCount_excel = worksheet.Dimension.Columns;   // excel总列数
                        int rowCount_datagridview = rowCount_excel - 1;     // datagridview总行数，datagridview里有自带的行标题，因此要少一行
                        int colCount_datagridview = colCount_excel - 1;     // datagridview总列数，datagridview里有自带的列标题，因此要少一列

                        // 设置行列数显示为当前表格大小值
                        row_set_textbox.Text = rowCount_datagridview.ToString();
                        col_set_textbox.Text = colCount_datagridview.ToString();

                        //创建datagridview
                        datagridview_creat(rowCount_datagridview, colCount_datagridview);

                        // 读取数据，设置禁用单元格。 从第二行，第二列开始读取数据
                        //Debug.WriteLine(worksheet.Cells[1, 1].Style.Fill.BackgroundColor.Rgb);
                        for (int row = 2; row <= rowCount_excel; row++)
                        {

                            for (int col = 2; col <= colCount_excel; col++)
                            {
                                var cell_excel = worksheet.Cells[row, col];                          // excel的单元格，索引从1开始
                                var cell_datagridview = dataGridView1.Rows[row - 2].Cells[col - 2];      // datagridview的单元格，索引从0开始。比excel的少一行和一列


                                // 读取单元背景颜色
                                var bgcolor = cell_excel.Style.Fill.BackgroundColor;
                                var fill = cell_excel.Style.Fill;

                                // 判断是否有填充颜色
                                if (fill.PatternType == OfficeOpenXml.Style.ExcelFillStyle.Solid)
                                { 
                                    //要判断是否填充的是普通颜色，还是主题颜色
                                    if (bgcolor.Theme.HasValue)  //如果是主题颜色
                                    {
                                        // 主题色转换为RBG颜色很麻烦，要自己建立基色表。这里统一填充为浅蓝色
                                        //Debug.WriteLine(bgcolor.Theme.Value);
                                        //Debug.WriteLine(bgcolor.Tint);
                                        cell_datagridview.Style.BackColor = Color.LightBlue;
                                    }
                                    else            //非主题颜色
                                    {
                                        if (bgcolor.Rgb == "FFD3D3D3")   // 单元格颜色是浅灰色（RGB都是211）
                                        {
                                            // 禁用单元格
                                            cell_datagridview.ReadOnly = true;
                                            cell_datagridview.Style.BackColor = Color.LightGray;    // 浅绿色
                                        }
                                        else        //如果不是浅灰色
                                        {
                                            // 把excel颜色（AARRGGBB格式），转换为颜色后赋予到datagridview
                                            cell_datagridview.Style.BackColor = ColorTranslator.FromHtml("#" + bgcolor.Rgb.Substring(2));
                                        }
                                    }

                                }



                                // 判断是否空值
                                if (cell_excel.Value != null)
                                {
                                    // 判断是否数值
                                    if (double.TryParse(cell_excel.Value?.ToString(), out double importValue))
                                    {
                                        // 如果是数值，写入到value，同步保存到tag用于后续做对比
                                        cell_datagridview.Value = importValue;
                                        //cell_datagridview.Tag = importValue;   // 同步保存到tag，用于后续对比

                                        Debug.Print(cell_excel.Value.ToString());
                                    }
                                    else
                                    {
                                        // 如果不是数值
                                        cell_datagridview.Value = cell_excel.Value;
                                        //cell_datagridview.Tag = null;

                                        Debug.Print(cell_excel.Value.ToString());
                                    
                                         //也可以在导入时将GND格子背景色填充为浅灰，这样就可以直接禁用单元格
                                        if (cell_excel.Value.ToString() == "GND")   // 如果单元格是GND就设置为禁用，跳过写入
                                        {
                                            // 禁用单元格
                                            cell_datagridview.ReadOnly = true;
                                            cell_datagridview.Style.BackColor = Color.LightGreen;    // 浅灰色对应的AARRGGBB就是FFFFFFFF（RGB都是211）
                                        }
                                    
                                    }
                                }else
                                {

                                }
                                    

                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入数据失败，请检查excel格式: {ex.Message}");
                }

            }

        }
        #endregion



        #region 导入数据对比按键函数(导入到datagridview的tag属性里)
        //导入对比标准按键点击事件
        private void inportCompare_btn_Click(object sender, EventArgs e)
        {
            //打开对话框，选择excel模板文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel文件|*.xlsx;*.xls";
            openFileDialog.Title = "选择Excel文件";


            string filePath = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                //获取excel模板路劲
                filePath = openFileDialog.FileName;

                try
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(filePath)))
                    {
                        // 获取第一个工作表
                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
                        //excel没有数据就提示错误
                        if (worksheet.Dimension == null)
                        {
                            MessageBox.Show("Excel 模板中没有数据");
                            return;
                        }

                        // 获取行数和列数
                        int rowCount_excel = worksheet.Dimension.Rows;      // excel总行数
                        int colCount_excel = worksheet.Dimension.Columns;   // excel总列数
                        int rowCount_datagridview = rowCount_excel - 1;     // datagridview总行数，datagridview里有自带的行标题，因此要少一行
                        int colCount_datagridview = colCount_excel - 1;     // datagridview总列数，datagridview里有自带的列标题，因此要少一列

                        // 读取数据，设置禁用单元格。 从第二行，第二列开始读取数据
                        //Debug.WriteLine(worksheet.Cells[1, 1].Style.Fill.BackgroundColor.Rgb);
                        for (int row = 2; row <= rowCount_excel; row++)
                        {

                            for (int col = 2; col <= colCount_excel; col++)
                            {
                                var cell_excel = worksheet.Cells[row, col];                          // excel的单元格，索引从1开始
                                var cell_datagridview = dataGridView1.Rows[row - 2].Cells[col - 2];  // datagridview的单元格，索引从0开始。比excel的少一行和一列

                                // 空值来判定是否禁用
                                // excel模板里面，单元格用空值来作为禁用规则
                                if (cell_excel.Value == null || string.IsNullOrWhiteSpace(cell_excel.Value.ToString()))
                                {
                                    // 禁用
                                }
                                else
                                {
                                    // 判断是否数值
                                    if (double.TryParse(cell_excel.Value?.ToString(), out double importValue))
                                    {
                                        // 如果是数值，写入到tag用于后续做对比
                                        cell_datagridview.Tag = importValue;   // 同步保存到tag，用于后续对比
                                        CompareAndMarkCell(cell_datagridview); // 马上比较一次颜色
                                    }
                                    else
                                    {
                                        // 如果不是数值
                                        cell_datagridview.Tag = null;
                                    }

                                }




                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入对比失败，请检查excel格式: {ex.Message}");
                }

            }

        }
        #endregion



        #region 复制粘贴函数
        // 复制选中单元格（右键菜单）
        private void CopySelectedCells()
        {
            if (dataGridView1.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                int minRow = dataGridView1.SelectedCells.Cast<DataGridViewCell>().Min(c => c.RowIndex);
                int maxRow = dataGridView1.SelectedCells.Cast<DataGridViewCell>().Max(c => c.RowIndex);
                int minCol = dataGridView1.SelectedCells.Cast<DataGridViewCell>().Min(c => c.ColumnIndex);
                int maxCol = dataGridView1.SelectedCells.Cast<DataGridViewCell>().Max(c => c.ColumnIndex);

                StringBuilder sb = new StringBuilder();
                for (int i = minRow; i <= maxRow; i++)
                {
                    List<string> rowValues = new List<string>();
                    for (int j = minCol; j <= maxCol; j++)
                    {
                        var cell = dataGridView1.Rows[i].Cells[j];
                        rowValues.Add(cell.Value?.ToString() ?? "");
                    }
                    sb.AppendLine(string.Join("\t", rowValues));
                }

                Clipboard.SetText(sb.ToString());
            }
        }


        // 粘贴数据到当前单元格（右键菜单）
        private void PasteToSelectedCell()
        {
            if (dataGridView1.CurrentCell == null)
                return;

            string clipboardText = Clipboard.GetText();
            string[] lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int startRow = dataGridView1.CurrentCell.RowIndex;
            int startCol = dataGridView1.CurrentCell.ColumnIndex;

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                string[] cells = lines[i].Split('\t');

                for (int j = 0; j < cells.Length; j++)
                {
                    int row = startRow + i;
                    int col = startCol + j;

                    if (row < dataGridView1.RowCount && col < dataGridView1.ColumnCount)
                    {
                        var targetCell = dataGridView1.Rows[row].Cells[col];
                        if (!targetCell.ReadOnly) // 跳过禁用单元格
                            targetCell.Value = cells[j];
                    }
                }
            }
        }
        #endregion



        #region 打开串口
        // 打开串口点击事件
        private void open_serialport_Click(object sender, EventArgs e)
        {
            try
            {
                if (open_serialport.Text == "连接设备")
                {
                    // 设置串口参数
                    string com_temp = "";
                    int dashIndex = serialport_select.Text.IndexOf('-');

                    if (dashIndex > 0)
                    {
                        // 提取'-'之前的部分并去除空格
                        com_temp = serialport_select.Text.Substring(0, dashIndex).Trim();
                    }


                    serialPort.PortName = com_temp;      // 串口号
                    serialPort.BaudRate = 115200;          // 波特率

                    // 打开串口
                    serialPort.Open();

                    //文字改为关闭
                    open_serialport.Text = "关闭设备";
                    open_serialport.BackColor = Color.Red;

                }
                else if (open_serialport.Text == "关闭设备")
                {
                    serialPort.Close();

                    //文字改为关闭
                    open_serialport.Text = "连接设备";
                    open_serialport.BackColor = Color.PaleGreen;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"设备连接错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        //刷新串口点击事件
        private void reflash_COM_Click(object sender, EventArgs e)
        {
            serialport_select.Items.Clear();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                // 获取详细信息（通过WMI）
                string description = GetPortDescription(port);
                string com_tepm = port + "-" + description;
                serialport_select.Items.Add(com_tepm);
            }
        }


        // 获取串口详细信息（需要 System.Management 引用）
        private string GetPortDescription(string portName)
        {
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");

                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["Caption"] != null)
                    {
                        string caption = obj["Caption"].ToString();
                        if (caption.Contains(portName))
                        {
                            return caption;
                        }
                    }
                }
            }
            catch
            {
                // 如果无法获取详细信息，返回空
            }
            return "未知设备";
        }
        #endregion



        #region 写入数据按键事件
        // 写入数据按键事件
        // 全局变量记录当前位置
        private int currentRow = 0;
        private int currentCol = 0;

        private bool _isWriting = false; //是否正在写入数据状态
        private bool switchvertical = false; //是否切换到竖向写入。默认为横向写入
        private async void write_btn_Click(object sender, EventArgs e)
        {
            if (_isWriting) return;   // 防止重复进入
            _isWriting = true;          //状态切换为正在写入
            write_btn.Enabled = false;      //点击后先禁用按钮，仿真快速点击误响应

            try
            {
                
                //发送串口读取命令
                serialPort.Write("READ_ON");  // 发送文本

                System.Threading.Thread.Sleep(100);  //延迟一段时间，避免串口数据还没传回来

                //获取串口回来的数据
                string data_receive = serialPort.ReadExisting();  // 读取所有可用数据
                data_receive = data_receive.Trim(); // 去掉空格、\r、\n
                double.TryParse(data_receive, out double data_receive_double);  //转换为double数值
                

                //double data_receive_double = 1;  //调试代码

                //将数据写入dataGridView1
                if (dataGridView1.ColumnCount == 0 || dataGridView1.RowCount == 0)
                    return;

                int totalRows = dataGridView1.RowCount;
                int totalCols = dataGridView1.ColumnCount;

                // 如果用户手动选中了单元格，就从选中单元格开始
                if (dataGridView1.CurrentCell != null)
                {
                    currentRow = dataGridView1.CurrentCell.RowIndex;
                    currentCol = dataGridView1.CurrentCell.ColumnIndex;
                }

                int attempts = 0; // 防止无限循环
                while (attempts < totalRows * totalCols)
                {
                    var cell = dataGridView1.Rows[currentRow].Cells[currentCol];

                    // 如果单元格可写
                    if (!cell.ReadOnly)
                    {
                        if (data_receive_double > 2.2)
                        {
                            cell.Value = "OL";                      // 数据大于2.2V，就判断为OL
                            cell.Style.BackColor = Color.Orange;    // 橙色
                        }
                        else
                        {
                            cell.Value = data_receive_double;       // 数据小于2.2V，正常写入
                        }
                        

                        //判断横向写入还是竖向写入，切换到下一个格子
                        if (!switchvertical)   // 横向写入
                        {
                            currentCol++;
                            if (currentCol >= totalCols)
                            {
                                currentCol = 0;
                                currentRow++;
                                if (currentRow >= totalRows)
                                    currentRow = 0;
                            }
                        }
                        else                 // 竖向写入
                        {
                            currentRow++;
                            if (currentRow >= totalRows)
                            {
                                currentCol++;
                                currentRow = 0;
                                if (currentCol >= totalCols)
                                    currentCol = 0;
                            }
                        }


                        // 设置 CurrentCell 可视化
                        dataGridView1.CurrentCell = dataGridView1.Rows[currentRow].Cells[currentCol];
                        break;
                    }
                    else
                    {
                        // 不写数据，直接跳过禁用单元格
                        //判断横向写入还是竖向写入，切换到下一个格子
                        if (!switchvertical)   // 横向写入
                        {
                            currentCol++;
                            if (currentCol >= totalCols)
                            {
                                currentCol = 0;
                                currentRow++;
                                if (currentRow >= totalRows)
                                    currentRow = 0;
                            }
                        }
                        else                 // 竖向写入
                        {
                            currentRow++;
                            if (currentRow >= totalRows)
                            {
                                currentCol++;
                                currentRow = 0;
                                if (currentCol >= totalCols)
                                    currentCol = 0;
                            }
                        }
                    }

                    attempts++;
                }

                // 写完数据播放提示音
                System.Media.SystemSounds.Exclamation.Play();  // 默认提示音
                //System.Media.SystemSounds.Hand.Play();      // 手型提示音（错误音）

            }
            catch (Exception ex)
            {
                MessageBox.Show($"请先连接设备：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await Task.Delay(300);          // 过滤连点 / 抖动

                write_btn.Enabled = true;      //结束后再把按键启用
                _isWriting = false;
            }

        }
        #endregion



        #region 按键切换写入数据方向，横向或竖向
        // 按键切换写入数据方向，横向或竖向
        private void switchvertical_btn_Click(object sender, EventArgs e)
        {
            if (switchvertical)
            {
                switchvertical = false;
                switchvertical_btn.Text = "横向";
            }
            else
            {
                switchvertical = true;
                switchvertical_btn.Text = "竖向";
            }
        }
        #endregion



        #region datagridview表格数据变化触发事件，数据对比函数（标记颜色）
        // datagridview 数据变化触发事件---用于比较数据，标记颜色
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            CompareAndMarkCell(cell);
        }


        // 数据对比判断，用于标记颜色
        private double compareValue = 0.1; // 比较阈值全局变量，默认为0.1
        private void CompareAndMarkCell(DataGridViewCell cell)
        {
            // 禁用或无值，不处理
            if (cell.ReadOnly || cell.Value == null)
                return;

            // 必须有基准值
            if (!(cell.Tag is double oldValue))
                return;

            // 当前值必须是数值
            if (!double.TryParse(cell.Value.ToString(), out double currentValue))
                return;

            // double diff = Math.Abs(currentValue - oldValue);  // 绝对值
            double diff = currentValue - oldValue;     // 差值
            if (diff >= compareValue)
            {
                // 超过阈值，标记为橙色
                cell.Style.BackColor = Color.Orange;
            }
            else if (diff <= -compareValue)
            {
                // 低于阈值（负数），标记为黄色
                cell.Style.BackColor = Color.Yellow;
            }
            else
            {
                // 在范围内就为白色
                cell.Style.BackColor = Color.White;
            }
        }
        #endregion



        #region 导出数据到excel的函数
        // 输出导出函数
        private void ExportToExcelWithEPPlus()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel文件|*.xlsx";
                saveFileDialog.Title = "保存Excel文件";
                saveFileDialog.FileName = "DataExport.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (ExcelPackage excelPackage = new ExcelPackage())
                        {
                            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet1");

                            // 设置行标题列（如果DataGridView显示行标题）
                            int startColIndex = 1;
                            bool includeRowHeaders = dataGridView1.RowHeadersVisible;


                            //excel的index从1开始。datagridview的index从0开始
                            //excel的第一行和第一列用来存储行标题和列标题
                            // 导出列标题
                            for (int i = 0; i < dataGridView1.Columns.Count; i++)
                            {
                                //获取列标题数据（datagridview的index从0开始），并将数据写入excel（excel的列标题index从2开始）
                                worksheet.Cells[1, i + 2].Value = dataGridView1.Columns[i].HeaderText;
                            }

                            // 导出行标题
                            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                            {
                                // 导出行标题
                                if (dataGridView1.Rows[i].HeaderCell != null &&
                                    dataGridView1.Rows[i].HeaderCell.Value != null)
                                {
                                    //获取行标题数据（datagridview的index从0开始），并将数据写入excel（excel的行标题index从2开始）
                                    worksheet.Cells[i + 2, 1].Value = dataGridView1.Rows[i].HeaderCell.Value.ToString();
                                }
                                else
                                {
                                    // 如果没有显式设置行标题，使用行号
                                    worksheet.Cells[i + 2, 1].Value = (i + 1).ToString();
                                }
                            }

                            // 导出数据 + 单元格颜色
                            for (int row = 0; row < dataGridView1.Rows.Count; row++)
                            {
                                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                                {
                                    var dgvCell = dataGridView1.Rows[row].Cells[col];  // datagridview单元格
                                    var excelCell = worksheet.Cells[row + 2, col + 2]; // Excel 从第2行第2列开始写入数据。

                                    // ===== 写入值 =====
                                    if (dgvCell.Value != null)
                                    {
                                        if (double.TryParse(dgvCell.Value.ToString(), out double num))
                                        {
                                            excelCell.Value = num;
                                            excelCell.Style.Numberformat.Format = "0.000";
                                        }
                                        else
                                        {
                                            excelCell.Value = dgvCell.Value.ToString();
                                        }
                                    }
                                    else
                                    {
                                        excelCell.Value = "";
                                    }

                                    // ===== 写入背景色 =====
                                    Color backColor = dgvCell.Style.BackColor;

                                    // 如果没有显式设置颜色，DataGridView 会返回 Empty
                                    if (!backColor.IsEmpty)
                                    {
                                        excelCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        excelCell.Style.Fill.BackgroundColor.SetColor(backColor);
                                    }

                                }
                            }



                            // 设置所有单元格的基本样式
                            if (worksheet.Dimension != null)
                            {
                                var allCells = worksheet.Cells[worksheet.Dimension.Address];

                                // 1. 设置列宽
                                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                                {
                                    worksheet.Column(col).Width = 7;
                                }


                                // 2. 设置行高
                                for (int i = 1; i <= worksheet.Dimension.Rows; i++)
                                {
                                    // 为每行设置相同的固定宽度
                                    worksheet.Row(i).Height = 35; // 数据行

                                    // 为每行设置数字数据格式为3位小数
                                    worksheet.Row(i).Style.Numberformat.Format = "0.000";
                                }

                                // 3. 设置边框
                                allCells.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                                allCells.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                                allCells.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                                allCells.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                                // 4. 设置居中
                                allCells.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                allCells.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                                // 5. 首行颜色
                                var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns];
                                headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                                // 5. 首列颜色
                                var firstColumn = worksheet.Cells[1, 1, worksheet.Dimension.Rows, 1];
                                firstColumn.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                firstColumn.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            }


                            // 保存文件
                            FileInfo excelFile = new FileInfo(saveFileDialog.FileName);
                            excelPackage.SaveAs(excelFile);

                            MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion



        #region 保存按键点击事件
        //保存按键点击事件
        private void save_btn_Click(object sender, EventArgs e)
        {
            ExportToExcelWithEPPlus();
        }
        #endregion



        #region 校验点击事件        
        //校准点击按键事件
        private void train_btn_Click(object sender, EventArgs e)
        {
            try
            {


                //发送串口读取命令
                string train_value = train_textbox.Text.Trim();  //获取校验值
                serialPort.Write($"TRAIN_{train_value}");        // 发送校验命令

                System.Threading.Thread.Sleep(100);              //延迟一段时间，避免串口数据还没传回来

                //获取串口回来的数据
                string data_receive = serialPort.ReadExisting();  // 读取所有可用数据
                data_receive = data_receive.Trim(); // 去掉空格、\r、\n
                double.TryParse(data_receive, out double data_receive_double);  //转换为double数值

                MessageBox.Show($"校准值为：{data_receive_double}", "校准成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"请先连接设备：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion



        #region 语音控制
        // 语音控制
        private SpeechRecognitionEngine recognizer;
        private bool voiceEnabled = false;   // 语音开关状态
        // 初始化语音
        private void InitSpeechRecognition()
        {
            try
            {
                recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("zh-CN"));

                // 定义命令
                Choices commands = new Choices();
                commands.Add(new string[] { "测试" });

                GrammarBuilder gb = new GrammarBuilder();
                gb.Append(commands);

                Grammar grammar = new Grammar(gb);
                recognizer.LoadGrammar(grammar);

                recognizer.SetInputToDefaultAudioDevice(); // 默认麦克风
                recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化语音识别失败，请确认是否插入麦克风：" + ex.Message);
            }
        }

        // 语音识别回调（触发写数据）
        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // 置信度过滤（很重要，防误触）
            if (e.Result.Confidence < 0.3)
                return;

            if (e.Result.Text == "测试")
            {
                // 切回 UI 线程
                this.BeginInvoke(new Action(() =>
                {
                    write_btn_Click(null, null);
                }));
            }
        }

        // 语音控制开关按钮点击
        private void voiceControl_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!voiceEnabled)
                {
                    InitSpeechRecognition();    // 初始化
                    recognizer.RecognizeAsync(RecognizeMode.Multiple);  // 启动识别
                    voiceEnabled = true;

                    voicestate_RioBtn.Text = "已开启";
                    voicestate_RioBtn.Checked = true;
                    voiceControl_btn.BackColor = Color.PaleGreen;
                }
                else
                {
                    recognizer.RecognizeAsyncStop();    //暂停
                    recognizer.Dispose();   // 关闭，释放资源
                    voiceEnabled = false;

                    voicestate_RioBtn.Text = "已关闭";
                    voicestate_RioBtn.Checked = false;
                    voiceControl_btn.BackColor = Color.White;

                }
            }
            catch (InvalidOperationException)
            {
                // 
            }


        }
        #endregion



        #region 软件使用说明
        // 软件使用说明
        /*
         *  1. 表格设置
            • <生成表格>：行数和列数输入数字，点击<设置>按钮生成表格。
            • <清除数据>：清除当前表格数据（禁用单元格不影响）。
            • <切换方向>：点击切换记录数据的方向，横向或者纵向。
            • <禁用单元格>：选中单元格后，按快捷键Ctrl+D可以将该单元格禁用。后续写入数据时会跳过该单元格。可以选择多个单元格一起禁用。

            2. 串口设置
            • <刷新串口>：插入USB设备后，点击刷新才能显示采集小板的串口。
            • <连接设备>：在下拉框选择采集小板对应的串口，点击连接设备。

            3. 数据操作
            • <写入>：点击按键，表格会将当前测试值记录到表格上，位置会从当前选择的单元格开始，方向参考前面<切换方向>
            • <保存>：保存当前记录的表格。测试未完成也可以先保存，后续再导入继续进行测试。
            • <导入>：导入前面未完成的数据或设置好的表格模版，然后继续测试。模版中的单元格如果需要禁用，要将填充背景颜色设置为浅灰色(R/G/B三个都为211)。如果单元格显示浅蓝色，可能原始表格使用的是主题色填充，需要更改
            • <对比>：如果需要实时对比数据，点击<对比>按钮将另一个样品的数据导入。注意对比样品的表格格式需要一致。一般是OK品数据。

            4. 校准设置
            • 如果测试过程中发现软件记录的值和万用表显示相差较大，可以做一次校准。
            • 校准方法为：先用万用表测试一个值，然后将这个值输入到输入框，最后点击<校准>按键（注意此时万用表需要一直在测着）。

            4. 语音控制
            • 点击<语音>按钮后打开语音控制。再点击关闭语音。
            • 打开语音控制后，可以说<测试>，软件会自动记录一次数据，替代点击写入功能。
            • 目前语音识别功能在不同电脑上体验不同，如果识别不准就不要用了。
         * 
         * 
         * 
         * 
         */
        //内容太多，只显示关键的吧
        private void help_btn_Click(object sender, EventArgs e)
        {
            string helpText = @"关键功能
1. 表格设置
• <切换方向>：点击切换记录数据的方向，横向或者纵向。
• <禁用单元格>：选中单元格后，按快捷键Ctrl+D可以将该单元格禁用。后续写入数据时会跳过该单元格。

2. 数据操作
• <保存>：保存当前记录的表格。测试未完成也可以先保存，后续再导入继续进行测试。
• <导入>：导入前面未完成的数据或设置好的表格模版，然后继续测试。模版中的单元格如果需要禁用，要将填充背景颜色设置为浅灰色(R/G/B三个都为211)。如果单元格显示浅蓝色，可能原始表格使用的是主题色填充，需要更改
• <对比>：如果需要实时对比数据，点击<对比>按钮将另一个样品的数据导入。注意对比样品的表格格式需要一致。一般是OK品数据。

3. 校准设置
• 如果测试过程中发现软件记录的值和万用表显示相差较大，可以做一次校准。

4. 语音控制
• 打开语音控制后，可以说<测试>，软件会自动记录一次数据，替代点击写入功能。
• 目前语音识别功能在不同电脑上体验不同，如果识别不准就不要用了。
";

            MessageBox.Show(helpText, "软件使用说明",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion



        #region 程序退出时弹出对话框
        //程序退出时提示保存
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 弹出提示框
            DialogResult result = MessageBox.Show(
                "是否保存数据？",
                "提示",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 用户选择保存
                ExportToExcelWithEPPlus();
            }
            else if (result == DialogResult.Cancel)
            {
                // 用户选择取消关闭
                e.Cancel = true; // 阻止窗口关闭
            }
            // 如果选择 No，则直接关闭，不保存

        }

        #endregion
   
    
    }
}
