using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CINCROSS;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		Application.ThreadException += (sender, e) =>
		{
			HandleUnhandled(e.Exception);
		};
		AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleUnhandled(e.ExceptionObject as Exception, fatal: true);
		TaskScheduler.UnobservedTaskException += (sender, e) =>
		{
			e.SetObserved();
			HandleUnhandled(e.Exception);
		};
		ApplicationConfiguration.Initialize();
		Application.Run(new Form1());
	}

	private static void HandleUnhandled(Exception? ex, bool fatal = false)
	{
		if (ex == null) return;
		Logger.Error(fatal ? "Program.UnhandledException" : "Program.ThreadException", ex);
		try
		{
			MessageBox.Show(
				ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace,
				"CINCROSS - Error inesperado",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
		catch
		{
		}
	}
}
