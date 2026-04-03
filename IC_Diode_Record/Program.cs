using OfficeOpenXml;

namespace IC_Diode_Record
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        /// 

        private static Mutex _mutex;

        [STAThread]
        static void Main()
        {
            // 设置 EPPlus 非商业许可
            ExcelPackage.License.SetNonCommercialPersonal("<Enic>");


            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            bool createdNew;

            _mutex = new Mutex(
                true,
                "IC_Diode_Record_Enic",
                out createdNew
            );

            if (!createdNew)
            {
                MessageBox.Show(
                    "程序已经在运行中。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);    // 禁用自动 DPI 缩放
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
        }
    }
}