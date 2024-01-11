namespace VHTapApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            RestartOnUnhandledException();
            ApplicationConfiguration.Initialize();
            Application.Run(new VHTapForm());
        }

        private static void RestartOnUnhandledException()
        {
            Application.ThreadException += (sender, e) =>
            {
                Console.WriteLine(e.Exception);
                Application.Restart();
                Environment.Exit(1);
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine(e.ExceptionObject);
                Application.Restart();
                Environment.Exit(1);
            };
        }

    }
}