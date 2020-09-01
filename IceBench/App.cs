using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace IceBench
{
	public class App : Application
	{
		private static Mutex mutex;

		public static bool IsNewInstance { get; private set; } = false;

		[STAThread]
		public static void Main()
		{
			bool isNewlyCreated = false;
			mutex = new Mutex(true, "IceBench", out isNewlyCreated);
			IsNewInstance = !isNewlyCreated;

			App app = new App();
			app.Run(new MainWindow());
		}
	}
}
