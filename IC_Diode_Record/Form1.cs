using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using Microsoft.CognitiveServices.Speech;
using System.Text.RegularExpressions;


namespace IC_Diode_Record
{
    /// <summary>
    /// 主窗体：表格创建/导入导出、禁用规则、语音写入、对比着色等业务入口。
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>初始化窗体与设计器组件。</summary>
        public Form1()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.None;        //Form 的 AutoScaleMode 设置为 None

        }




        /// <summary>
        /// 窗体加载事件：初始化右键菜单、滚动保护事件、默认写入方向。
        /// </summary>
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

            // 写入方向默认值
            direction_comboBox.SelectedIndex = 0;

        }



        #region keypress函数。限制textbox只能输入数字、小数
        /// <summary>
        /// 仅允许 TextBox 输入数字（含控制键），其余字符阻止。
        /// </summary>
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

        #endregion



        #region 重置datagridview函数
        /// <summary>
        /// 重置 DataGridView：清结构、清选择、恢复默认样式与行为。
        /// </summary>
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
        /// <summary>
        /// 「设置」按钮：确认后按输入行列重建表格并重置写入游标。
        /// </summary>
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
        /// <summary>
        /// 根据行列构建表格并设置标题、字体、复制粘贴行为与窗口尺寸。
        /// </summary>
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

        /// <summary>
        /// 根据网格内容计算并调整 DataGridView 与窗体尺寸（带上下限）。
        /// </summary>
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
        /// <summary>编辑开始前记录的面板滚动位置。</summary>
        private Point _panelScrollPos;
        /// <summary>当前是否处于单元格编辑态（用于滚动保护）。</summary>
        private bool _isEditing = false;
        /// <summary>开始编辑单元格时缓存滚动位置。</summary>
        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            _isEditing = true;
            _panelScrollPos = panel1.AutoScrollPosition;
        }
        /// <summary>结束编辑时恢复滚动位置。</summary>
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            _isEditing = false;

            // 仅在编辑时恢复滚动
            panel1.AutoScrollPosition = new Point(-_panelScrollPos.X, -_panelScrollPos.Y);
        }
        /// <summary>编辑过程中切换当前单元格时继续保持滚动位置。</summary>
        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (_isEditing)
            {
                // 编辑过程中保持滚动
                panel1.AutoScrollPosition = new Point(-_panelScrollPos.X, -_panelScrollPos.Y);
            }
        }
        #endregion



        #region datagridview按键函数（F1 禁用当前格并下一格、Ctrl+D、Ctrl+C/V）
        /// <summary>
        /// 表格快捷键入口：F1 禁用并前进，Ctrl+D 切换禁用，Ctrl+C/V 复制粘贴。
        /// </summary>
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            // F1：禁用当前单元格，并按当前写入方向跳到下一格
            if (e.KeyCode == Keys.F1)
            {
                DisableCurrentCellAndAdvanceToNext();
                e.Handled = true;
                return;
            }

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
        /// <summary>
        /// 对当前选区批量切换禁用状态：有任一可写则全禁用，否则全恢复可写。
        /// </summary>
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

        /// <summary>
        /// 禁用当前单元格并按写入方向前进一格（F1/语音「跳过」共用）。
        /// </summary>
        private void DisableCurrentCellAndAdvanceToNext()
        {
            DisableConsecutiveCellsAlongDirection(1, skipToNextWritableAfter: false);
        }

        /// <summary>
        /// 沿写入方向连续禁用 count 格；可选再跳到下一可写格（用于「跳过N个」）。
        /// </summary>
        private void DisableConsecutiveCellsAlongDirection(int count, bool skipToNextWritableAfter)
        {
            if (count < 1 || dataGridView1.CurrentCell == null)
                return;

            int totalRows = dataGridView1.RowCount;
            int totalCols = dataGridView1.ColumnCount;
            if (totalRows <= 0 || totalCols <= 0)
                return;

            int maxSteps = totalRows * totalCols;
            count = Math.Min(count, maxSteps);

            for (int i = 0; i < count; i++)
            {
                var cell = dataGridView1.CurrentCell;
                if (cell.RowIndex < 0 || cell.ColumnIndex < 0)
                    return;

                cell.ReadOnly = true;
                cell.Style.BackColor = Color.LightGray;
                cell.Tag = "DISABLED";

                currentRow = cell.RowIndex;
                currentCol = cell.ColumnIndex;
                NormalizeCurrentCellForDirection(totalRows, totalCols);
                AdvanceToNextCell(totalRows, totalCols);

                if (currentRow < 0 || currentRow >= totalRows || currentCol < 0 || currentCol >= totalCols)
                    return;
                dataGridView1.CurrentCell = dataGridView1.Rows[currentRow].Cells[currentCol];
            }

            if (!skipToNextWritableAfter)
                return;

            for (int s = 0; s < maxSteps; s++)
            {
                var c = dataGridView1.Rows[currentRow].Cells[currentCol];
                if (!c.ReadOnly)
                    break;
                AdvanceToNextCell(totalRows, totalCols);
                if (currentRow < 0 || currentRow >= totalRows || currentCol < 0 || currentCol >= totalCols)
                    return;
            }

            dataGridView1.CurrentCell = dataGridView1.Rows[currentRow].Cells[currentCol];
        }
        #endregion



        #region 清除按键点击事件---清除单元格数据，不清除禁用结构
        /// <summary>清空所有单元格值，不改变禁用结构与颜色规则。</summary>
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
        /// <summary>
        /// 导入模板数据到表格：读取值、颜色、禁用信息，并按模板尺寸重建网格。
        /// </summary>
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

                                    }
                                    else
                                    {
                                        // 如果不是数值
                                        cell_datagridview.Value = cell_excel.Value;
                                        //cell_datagridview.Tag = null;

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
        /// <summary>
        /// 导入对比基准：把 Excel 数值写入单元格 Tag，并立即触发一次颜色比较。
        /// </summary>
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
        /// <summary>复制当前选区为制表符文本，写入系统剪贴板。</summary>
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


        /// <summary>从当前单元格起粘贴剪贴板内容，自动跳过禁用单元格。</summary>
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



        #region 表格写入方向
        /// <summary>写入游标所在行。</summary>
        private int currentRow = 0;
        /// <summary>写入游标所在列。</summary>
        private int currentCol = 0;

        /// <summary>语音写入节流锁，防止连续识别重复写入。</summary>
        private bool _isWriting = false;
        /// <summary>表格自动前进方向。</summary>
        private enum WriteDirection
        {
            Horizontal,
            Vertical,
            CounterClockwise
        }
        /// <summary>当前写入方向（默认横向）。</summary>
        private WriteDirection currentDirection = WriteDirection.Horizontal;

        /// <summary>判断单元格是否位于表格最外圈。</summary>
        private static bool IsPerimeterCell(int row, int col, int totalRows, int totalCols)
        {
            return row == 0 || col == 0 || row == totalRows - 1 || col == totalCols - 1;
        }

        /// <summary>构建逆时针最外圈路径（用于逆时针写入模式）。</summary>
        private static List<(int Row, int Col)> BuildCounterClockwisePerimeterPath(int totalRows, int totalCols)
        {
            var path = new List<(int Row, int Col)>();
            if (totalRows <= 0 || totalCols <= 0)
                return path;

            // 单行：从左到右
            if (totalRows == 1)
            {
                for (int c = 0; c < totalCols; c++)
                    path.Add((0, c));
                return path;
            }

            // 单列：从上到下
            if (totalCols == 1)
            {
                for (int r = 0; r < totalRows; r++)
                    path.Add((r, 0));
                return path;
            }

            // 左边：上 -> 下
            for (int r = 0; r < totalRows; r++)
                path.Add((r, 0));
            // 下边：左 -> 右（跳过左下角）
            for (int c = 1; c < totalCols; c++)
                path.Add((totalRows - 1, c));
            // 右边：下 -> 上（跳过右下角）
            for (int r = totalRows - 2; r >= 0; r--)
                path.Add((r, totalCols - 1));
            // 上边：右 -> 左（跳过右上角和左上角）
            for (int c = totalCols - 2; c >= 1; c--)
                path.Add((0, c));

            return path;
        }

        /// <summary>在逆时针模式下将内圈起点归一到外圈左上角。</summary>
        private void NormalizeCurrentCellForDirection(int totalRows, int totalCols)
        {
            if (currentDirection != WriteDirection.CounterClockwise)
                return;
            if (IsPerimeterCell(currentRow, currentCol, totalRows, totalCols))
                return;

            // 逆时针模式只写最外圈：若当前选择在内圈，自动跳到左上角外圈起点
            currentRow = 0;
            currentCol = 0;
        }

        /// <summary>按当前方向推进写入游标。</summary>
        private void AdvanceToNextCell(int totalRows, int totalCols)
        {
            switch (currentDirection)
            {
                case WriteDirection.Horizontal:
                    currentCol++;
                    if (currentCol >= totalCols)
                    {
                        currentCol = 0;
                        currentRow++;
                        if (currentRow >= totalRows)
                            currentRow = 0;
                    }
                    break;

                case WriteDirection.Vertical:
                    currentRow++;
                    if (currentRow >= totalRows)
                    {
                        currentCol++;
                        currentRow = 0;
                        if (currentCol >= totalCols)
                            currentCol = 0;
                    }
                    break;

                case WriteDirection.CounterClockwise:
                    var path = BuildCounterClockwisePerimeterPath(totalRows, totalCols);
                    if (path.Count == 0)
                        return;

                    int idx = path.FindIndex(p => p.Row == currentRow && p.Col == currentCol);
                    if (idx < 0)
                    {
                        currentRow = path[0].Row;
                        currentCol = path[0].Col;
                        return;
                    }

                    int nextIdx = (idx + 1) % path.Count;
                    currentRow = path[nextIdx].Row;
                    currentCol = path[nextIdx].Col;
                    break;
            }
        }
        

        #endregion



        #region 下拉列表切换写入方向（横向/竖向/逆时针）
        /// <summary>方向下拉框切换事件。</summary>
        private void direction_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDirection = direction_comboBox.SelectedIndex switch
            {
                1 => WriteDirection.Vertical,
                2 => WriteDirection.CounterClockwise,
                _ => WriteDirection.Horizontal
            };
        }
        #endregion



        #region datagridview表格数据变化触发事件，数据对比函数（标记颜色）
        /// <summary>单元格值变化时触发对比着色。</summary>
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            CompareAndMarkCell(cell);
        }

        /// <summary>对比阈值（当前值 - 基准值）。</summary>
        private double compareValue = 0.1;
        /// <summary>按阈值比较单元格 Value 与 Tag，并设置背景色。</summary>
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
        /// <summary>
        /// 导出当前表格到 Excel：值、行列头、背景色和基础样式一并输出。
        /// </summary>
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
        /// <summary>保存按钮事件：执行导出。</summary>
        private void save_btn_Click(object sender, EventArgs e)
        {
            ExportToExcelWithEPPlus();
        }
        #endregion



        #region 语音控制
        /// <summary>Azure 连续识别实例。</summary>
        private SpeechRecognizer? _azureRecognizer;
        /// <summary>语音识别开关状态（UI 与识别器状态同步）。</summary>
        private bool voiceEnabled = false;

        /// <summary>停止并释放当前识别器（幂等）。</summary>
        private void StopVoiceRecognitionIfRunning()
        {
            if (_azureRecognizer == null)
            {
                UpdateVoiceRawText(string.Empty);
                return;
            }
            try
            {
                _azureRecognizer.StopContinuousRecognitionAsync().Wait(TimeSpan.FromSeconds(12));
            }
            catch { /* */ }
            try
            {
                _azureRecognizer.Dispose();
            }
            catch { /* */ }
            _azureRecognizer = null;
            voiceEnabled = false;
            UpdateVoiceRawText(string.Empty);
        }

        /// <summary>将语音识别得到的读数写入当前流向的下一可写格，并前进。</summary>
        private void WriteMeasurementToGrid(object cellValue, bool olOrange)
        {
            if (dataGridView1.ColumnCount == 0 || dataGridView1.RowCount == 0)
                return;

            int totalRows = dataGridView1.RowCount;
            int totalCols = dataGridView1.ColumnCount;

            if (dataGridView1.CurrentCell != null)
            {
                currentRow = dataGridView1.CurrentCell.RowIndex;
                currentCol = dataGridView1.CurrentCell.ColumnIndex;
            }

            var startCell = dataGridView1.Rows[currentRow].Cells[currentCol];
            if (!startCell.ReadOnly)
                NormalizeCurrentCellForDirection(totalRows, totalCols);

            int attempts = 0;
            while (attempts < totalRows * totalCols)
            {
                var cell = dataGridView1.Rows[currentRow].Cells[currentCol];

                if (!cell.ReadOnly)
                {
                    if (olOrange)
                    {
                        cell.Value = "OL";
                        cell.Style.BackColor = Color.Orange;
                    }
                    else
                    {
                        cell.Value = cellValue;
                        //cell.Style.BackColor = Color.White;
                    }

                    AdvanceToNextCell(totalRows, totalCols);
                    dataGridView1.CurrentCell = dataGridView1.Rows[currentRow].Cells[currentCol];
                    break;
                }

                AdvanceToNextCell(totalRows, totalCols);
                attempts++;
            }

            System.Media.SystemSounds.Exclamation.Play();
        }

        /// <summary>语音写入提交（带短延时防抖）。</summary>
        private async void CommitVoiceMeasurementAsync(object cellValue, bool olOrange)
        {
            if (_isWriting) return;
            _isWriting = true;
            try
            {
                WriteMeasurementToGrid(cellValue, olOrange);
            }
            finally
            {
                await Task.Delay(300);
                _isWriting = false;
            }
        }

        /// <summary>将 Azure 返回的原文显示到界面上。</summary>
        private void UpdateVoiceRawText(string text)
        {
            if (voiceRaw_textBox.IsDisposed)
                return;

            if (voiceRaw_textBox.InvokeRequired)
            {
                voiceRaw_textBox.BeginInvoke(new Action(() => voiceRaw_textBox.Text = text));
            }
            else
            {
                voiceRaw_textBox.Text = text;
            }
        }

        /// <summary>
        /// Azure 识别结果回调：先解析跳过指令，再解析读数并写入表格。
        /// </summary>
        private void AzureRecognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            string raw = e.Result.Text ?? string.Empty;
            UpdateVoiceRawText(raw);

            if (e.Result.Reason != ResultReason.RecognizedSpeech)
                return;

            if (VoiceRecognitionParser.TryParseSkipCommand(e.Result.Text, out int skipCount))
            {
                bool skipToWritable = skipCount >= 2;
                this.BeginInvoke(new Action(() =>
                    DisableConsecutiveCellsAlongDirection(skipCount, skipToWritable)));
                return;
            }

            if (!VoiceRecognitionParser.TryParseMeasurement(e.Result.Text, out object? meas, out bool ol))
            {
                return;
            }

            this.BeginInvoke(new Action(() => CommitVoiceMeasurementAsync(meas!, ol)));
        }

        /// <summary>Azure 取消/错误回调（仅错误时提示）。</summary>
        private void AzureRecognizer_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason != CancellationReason.Error)
                return;
            this.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(
                    "Azure 语音识别错误：" + e.ErrorDetails,
                    "语音",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }));
        }

        /// <summary>打开 Azure 语音配置窗口并保存密钥/区域。</summary>
        private void voiceAzure_btn_Click(object sender, EventArgs e)
        {
            using var dlg = new VoiceAzureConfigForm(AzureVoiceSettings.Load());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            dlg.GetSettings().Save();
            MessageBox.Show("已保存。请点击「语音」开启识别，读出数值或「跳过」等指令。", "Azure 语音", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>开启/关闭 Azure 连续语音识别。</summary>
        private async void voiceControl_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!voiceEnabled)
                {
                    var settings = AzureVoiceSettings.Load();
                    if (!settings.IsValid)
                    {
                        MessageBox.Show("请先点击「密钥」，填写 Azure 语音资源的密钥与区域。", "Azure 语音", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    StopVoiceRecognitionIfRunning();

                    var speechConfig = SpeechConfig.FromSubscription(settings.SubscriptionKey, settings.Region);
                    speechConfig.SpeechRecognitionLanguage = "zh-CN";

                    SpeechRecognizer? recognizer = null;
                    try
                    {
                        recognizer = new SpeechRecognizer(speechConfig);
                        var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
                        foreach (var p in VoiceRecognitionParser.MeasurementPhraseHints)
                            phraseList.AddPhrase(p);
                        foreach (var p in VoiceRecognitionParser.SkipPhrases)
                            phraseList.AddPhrase(p);

                        recognizer.Recognized += AzureRecognizer_Recognized;
                        recognizer.Canceled += AzureRecognizer_Canceled;

                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(true);

                        _azureRecognizer = recognizer;
                        recognizer = null;
                        voiceEnabled = true;

                        voicestate_RioBtn.Text = "已开启";
                        voicestate_RioBtn.Checked = true;
                        voiceControl_btn.BackColor = Color.PaleGreen;
                    }
                    finally
                    {
                        recognizer?.Dispose();
                    }
                }
                else
                {
                    StopVoiceRecognitionIfRunning();

                    voicestate_RioBtn.Text = "已关闭";
                    voicestate_RioBtn.Checked = false;
                    voiceControl_btn.BackColor = Color.White;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动语音识别失败：" + ex.Message + "\r\n请检查密钥、区域与网络。", "Azure 语音", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopVoiceRecognitionIfRunning();
                voicestate_RioBtn.Text = "已关闭";
                voicestate_RioBtn.Checked = false;
                voiceControl_btn.BackColor = Color.White;
            }
        }
        #endregion



        #region 软件使用说明
        // 软件使用说明
        /*
         *  1. 表格设置
            • <生成表格>：行数和列数输入数字，点击<设置>按钮生成表格。
            • <清除数据>：清除当前表格数据（禁用单元格不影响）。
            • <切换方向>：通过下拉列表选择记录方向：横向、竖向、逆时针（仅最外圈）。
            • <禁用单元格>：选中单元格后，按快捷键Ctrl+D可以将该单元格禁用。后续写入数据时会跳过该单元格。可以选择多个单元格一起禁用。

            2. 数据操作
            • 读数写入：开启「语音」后，直接说出数值（如 1.5、12.3）、带阻抗单位（K/k/千、M/m/兆，如 1.5K），或 OL/开路/过载 等；会按当前方向写入当前格并前进。
            • <保存>：保存当前记录的表格。测试未完成也可以先保存，后续再导入继续进行测试。
            • <导入>：导入前面未完成的数据或设置好的表格模版，然后继续测试。模版中的单元格如果需要禁用，要将填充背景颜色设置为浅灰色(R/G/B三个都为211)。如果单元格显示浅蓝色，可能原始表格使用的是主题色填充，需要更改
            • <对比>：如果需要实时对比数据，点击<对比>按钮将另一个样品的数据导入。注意对比样品的表格格式需要一致。一般是OK品数据。

            3. 语音控制（需联网）
            • 点击<密钥>填写API，点击<语音>开启/关闭云端识别。
            • 可以说「跳过」：等同 F1，禁用当前格并前进一格；「跳过2个」「跳过两个」：沿方向禁用 2 格再跳到下一可写格；「跳过3个」等同理。

            4. 快捷键
            • 按"F1"可以禁用当前单元格并跳到下一个格子。

         * 
         * 
         * 
         * 
         */
        /// <summary>弹出简化版使用说明。</summary>
        private void help_btn_Click(object sender, EventArgs e)
        {
            string helpText = @"关键功能
1. 表格设置
• <切换方向>：通过下拉列表选择记录方向：横向、竖向、逆时针（仅最外圈）。
• <禁用单元格>：选中单元格后，按快捷键Ctrl+D可以将该单元格禁用。后续写入数据时会跳过该单元格。

2. 数据操作
• 语音读数：阿拉伯或中文整数/小数（如 123、1.23、一二三、一百二十三、十二点三四），可加 K/M/千/兆，或 OL/开路/过载；「跳过」支持中文数量（如 跳过二十三）。
• <保存>：保存当前记录的表格。测试未完成也可以先保存，后续再导入继续进行测试。
• <导入>：导入前面未完成的数据或设置好的表格模版，然后继续测试。模版中的单元格如果需要禁用，要将填充背景颜色设置为浅灰色(R/G/B三个都为211)。如果单元格显示浅蓝色，可能原始表格使用的是主题色填充，需要更改
• <对比>：如果需要实时对比数据，点击<对比>按钮将另一个样品的数据导入。注意对比样品的表格格式需要一致。一般是OK品数据。

3. 语音控制
• 点击<密钥>填写API，点击<语音>开启/关闭云端识别。
• 「跳过」等同 F1；「跳过2个」「跳过两个」「跳过二十三」等：按数量沿方向禁用格子。

4. 快捷键
• 按""F1""可以禁用当前单元格并跳到下一个格子。
";

            MessageBox.Show(helpText, "软件使用说明",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion



        #region 程序退出时弹出对话框
        /// <summary>
        /// 关闭窗体时提示是否保存；同时确保语音识别器已正确停止释放。
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopVoiceRecognitionIfRunning();

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
