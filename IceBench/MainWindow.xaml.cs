using Bundle;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IceBench
{
    public partial class MainWindow : Window
	{
		private IceServer iceServer;

		private IceClient iceClient;

		private string serverArgs;

		private string clientArgs;

		private int serverRestartIntervalMS;

		private int clientRestartIntervalMS;

		private bool isAsyncMode;

		private int contentSizeMB;

		private readonly List<string> messages;

		public MainWindow()
		{
			InitializeComponent();
			messages = new List<string>();
			textBoxServerArgs.Text = "default -h localhost -p 5050 -t 5000";
			buttonStartServer.IsEnabled = true;
			buttonStopServer.IsEnabled = false;
			buttonRestartServer.IsEnabled = false;
			textBoxClientArgs.Text = "default -h localhost -p 5050 -t 2000";
			buttonStartClient.IsEnabled = true;
			buttonStopClient.IsEnabled = false;
			buttonRestartClient.IsEnabled = false;
			textBoxServerRestartInterval.Text = "1000";
			textBoxClientRestartInterval.Text = "1000";
			textBoxContentSize.Text = "2";
			checkBoxAsync.IsChecked = false;
			buttonClearLogs.IsEnabled = false;
			buttonExportLogs.IsEnabled = false;
			textBoxContentSize.IsEnabled = false;
			iceServer = new IceServer();
			iceClient = new IceClient(isAsyncMode, contentSizeMB);
			iceServer.OnStatusChanged += IceServer_OnStatusChanged;
			iceServer.OnMethodInvoked += IceServer_OnMethodInvoked;
			iceServer.OnExceptionOccured += IceServer_OnExceptionOccured;
			iceClient.OnStatusChanged += IceClient_OnStatusChanged;
			iceClient.OnMethodInvoked += IceClient_OnMethodInvoked;
			iceClient.OnExceptionOccured += IceClient_OnExceptionOccured;
			IceServer_OnStatusChanged(iceServer.Status);
			IceClient_OnStatusChanged(iceClient.Status);

			if(App.IsNewInstance)
            {
				groupServer.Visibility = Visibility.Hidden;
				Title += " [Client ONLY]";
            }
		}

		private void IceServer_OnStatusChanged(BundleStatus status)
		{
			if(App.IsNewInstance)
            {
				return;
            }

			AddMessage($"Server Status: {status}");
			Application.Current.Dispatcher.Invoke(delegate
			{
				labelServerStatus.Content = $"{status}";
				labelServerStatus.Foreground = Brushes.SteelBlue;
				switch (status)
				{
				case BundleStatus.Unknown:
					labelServerStatus.Background = Brushes.Gray;
					labelServerStatus.Foreground = Brushes.White;
					break;
				case BundleStatus.Exception:
					labelServerStatus.Background = Brushes.OrangeRed;
					labelServerStatus.Foreground = Brushes.White;
					break;
				case BundleStatus.Running:
					labelServerStatus.Background = Brushes.LightGreen;
					labelServerStatus.Foreground = Brushes.DarkBlue;
					break;
				case BundleStatus.Stopped:
					labelServerStatus.Foreground = Brushes.Red;
					break;
				case BundleStatus.Idle:
					labelServerStatus.Background = Brushes.LightGray;
					labelServerStatus.Foreground = Brushes.DarkGreen;
					break;
				default:
					labelServerStatus.Background = Brushes.LightGoldenrodYellow;
					break;
				}
			});
		}

		private void IceServer_OnMethodInvoked(OperationType operation, bool isAsync)
		{
			AddMessage("Server Executing: " + operation + ", IsAsync: " + isAsync);
		}

		private void IceServer_OnExceptionOccured(Exception exception)
		{
			AddMessage($"Server Exception: {exception.Message}");
		}

		private void IceClient_OnStatusChanged(BundleStatus status)
		{
			AddMessage($"Client Status: {status}");
			Application.Current.Dispatcher.Invoke(delegate
			{
				if (status != BundleStatus.Unknown)
				{
					textBoxContentSize.IsEnabled = true;
				}
				labelClientStatus.Content = $"{status}";
				labelClientStatus.Foreground = Brushes.SteelBlue;
				switch (status)
				{
				case BundleStatus.Unknown:
					labelClientStatus.Background = Brushes.Gray;
					labelClientStatus.Foreground = Brushes.White;
					break;
				case BundleStatus.Exception:
					labelClientStatus.Background = Brushes.OrangeRed;
					labelClientStatus.Foreground = Brushes.White;
					break;
				case BundleStatus.Running:
					labelClientStatus.Background = Brushes.LightGreen;
					labelClientStatus.Foreground = Brushes.DarkBlue;
					break;
				case BundleStatus.Stopped:
					labelClientStatus.Foreground = Brushes.Red;
					break;
				case BundleStatus.Idle:
					labelClientStatus.Background = Brushes.LightGray;
					labelClientStatus.Foreground = Brushes.DarkGreen;
					break;
				default:
					labelClientStatus.Background = Brushes.LightGoldenrodYellow;
					break;
				}
			});
		}

		private void IceClient_OnMethodInvoked(OperationType operation, bool isAsync)
		{
			AddMessage("Client Calling:   " + operation + ", IsAsync: " + isAsync);
		}

		private void IceClient_OnExceptionOccured(Exception exception)
		{
			AddMessage($"Client Exception: {exception.Message}");
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (iceServer != null)
			{
				iceServer.OnStatusChanged -= IceServer_OnStatusChanged;
				iceServer.OnExceptionOccured -= IceServer_OnExceptionOccured;
				iceServer.OnMethodInvoked -= IceServer_OnMethodInvoked;
				iceServer.Exit();
			}
			if (iceClient != null)
			{
				iceClient.OnStatusChanged -= IceClient_OnStatusChanged;
				iceClient.OnExceptionOccured -= IceClient_OnExceptionOccured;
				iceClient.Exit();
			}
			base.OnClosing(e);
		}

		private void ServerArgs_TextChanged(object sender, TextChangedEventArgs e)
		{
			serverArgs = textBoxServerArgs.Text?.Trim();
		}

		private void ContentSize_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (int.TryParse(textBoxContentSize.Text, out contentSizeMB))
			{
				if (contentSizeMB > 0 && contentSizeMB < 256)
				{
					iceClient?.SetContentSize(contentSizeMB);
					return;
				}
			}

			contentSizeMB = 2;
			iceClient?.SetContentSize(contentSizeMB);
		}

		private void ClientArgs_TextChanged(object sender, TextChangedEventArgs e)
		{
			clientArgs = textBoxClientArgs.Text?.Trim();
		}

		private void ServerRestartInterval_TextChanged(object sender, TextChangedEventArgs e)
		{
			int.TryParse(textBoxServerRestartInterval.Text, out serverRestartIntervalMS);
			if (serverRestartIntervalMS < 100 || serverRestartIntervalMS > 60000)
			{
				textBoxServerRestartInterval.Text = "1000";
			}
		}

		private void ClientRestartInterval_TextChanged(object sender, TextChangedEventArgs e)
		{
			int.TryParse(textBoxClientRestartInterval.Text, out clientRestartIntervalMS);
			if (clientRestartIntervalMS < 100 || clientRestartIntervalMS > 60000)
			{
				textBoxClientRestartInterval.Text = "1000";
			}
		}

		private void BtnStartServer_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartServer.IsEnabled = true;
			buttonStartServer.IsEnabled = false;
			buttonStopServer.IsEnabled = true;
			AddMessage("User Start Server: " + serverArgs);
			Task.Run(delegate
			{
				iceServer.Start(serverArgs);
			});
		}

		private void BtnStopServer_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartServer.IsEnabled = true;
			buttonStartServer.IsEnabled = true;
			buttonStopServer.IsEnabled = false;
			AddMessage("User Stop Server");
			iceServer.Stop();
		}

		private void BtnRestartServer_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartServer.IsEnabled = false;
			buttonStartServer.IsEnabled = false;
			buttonStopServer.IsEnabled = false;
			AddMessage("User Restart Server: " + serverArgs);
			Task.Run(delegate
			{
				iceServer.OnStatusChanged -= IceServer_OnStatusChanged;
				iceServer.OnExceptionOccured -= IceServer_OnExceptionOccured;
				iceServer.OnMethodInvoked -= IceServer_OnMethodInvoked;
				iceServer.Exit();
				IceServer_OnStatusChanged(iceServer.Status);
				Thread.Sleep(serverRestartIntervalMS);
				iceServer = new IceServer();
				iceServer.OnStatusChanged += IceServer_OnStatusChanged;
				iceServer.OnExceptionOccured += IceServer_OnExceptionOccured;
				iceServer.OnMethodInvoked += IceServer_OnMethodInvoked;
				Application.Current.Dispatcher.Invoke(delegate
				{
					buttonStopServer.IsEnabled = true;
					buttonRestartServer.IsEnabled = true;
				});
				IceServer_OnStatusChanged(iceServer.Status);
				iceServer.Start(serverArgs);
			});
		}

		private void BtnThrowServerError_Click(object sender, RoutedEventArgs e)
		{
			buttonGenerateError.IsEnabled = false;
			iceServer?.SetUserRequestError();
			Task.Run(() =>
			{
				Thread.Sleep(1000);
				App.Current.Dispatcher.Invoke(() =>
				{
					buttonGenerateError.IsEnabled = true;
				});
			});
		}

		private void BtnStartClient_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartClient.IsEnabled = true;
			buttonStartClient.IsEnabled = false;
			buttonStopClient.IsEnabled = true;
			AddMessage("User Start Client: " + clientArgs);
			Task.Run(delegate
			{
				iceClient.Start(clientArgs);
			});
		}

		private void BtnStopClient_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartClient.IsEnabled = true;
			buttonStartClient.IsEnabled = true;
			buttonStopClient.IsEnabled = false;
			AddMessage("User Stop Client");
			iceClient.Stop();
		}

		private void BtnRestartClient_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartClient.IsEnabled = false;
			buttonStartClient.IsEnabled = false;
			buttonStopClient.IsEnabled = false;
			AddMessage("User Restart Client: " + clientArgs);
			Task.Run(delegate
			{
				iceClient.OnStatusChanged -= IceClient_OnStatusChanged;
				iceClient.OnExceptionOccured -= IceClient_OnExceptionOccured;
				iceClient.OnMethodInvoked -= IceClient_OnMethodInvoked;
				iceClient.Exit();
				IceClient_OnStatusChanged(iceClient.Status);
				Thread.Sleep(clientRestartIntervalMS);
				iceClient = new IceClient(isAsyncMode, contentSizeMB);
				iceClient.OnStatusChanged += IceClient_OnStatusChanged;
				iceClient.OnExceptionOccured += IceClient_OnExceptionOccured;
				iceClient.OnMethodInvoked += IceClient_OnMethodInvoked;
				Application.Current.Dispatcher.Invoke(delegate
				{
					buttonStopClient.IsEnabled = true;
					buttonRestartClient.IsEnabled = true;
				});
				IceClient_OnStatusChanged(iceClient.Status);
				iceClient.Start(clientArgs);
			});
		}

		private void CheckBoxAsync_Checked(object sender, RoutedEventArgs e)
		{
			isAsyncMode = true;
			iceClient.SetAsync(isAsyncMode);
		}

		private void CheckBoxAsync_Unchecked(object sender, RoutedEventArgs e)
		{
			isAsyncMode = false;
			iceClient.SetAsync(isAsyncMode);
		}

		private void AddMessage(string message)
		{
			string str = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] {message}";
			messages.Add(str);
			Application.Current.Dispatcher.Invoke(delegate
			{
				buttonClearLogs.IsEnabled = true;
				buttonExportLogs.IsEnabled = true;
				listViewMessages.Items.Add(str);
				listViewMessages.ScrollIntoView(str);
			});
		}

		private void BtnExportLogs_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "*.log|LOGS";
			saveFileDialog.FileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
			saveFileDialog.DefaultExt = "*.log";
			if (saveFileDialog.ShowDialog(this).GetValueOrDefault())
			{
				string fileName = saveFileDialog.FileName;
				if (File.Exists(fileName))
				{
					File.Delete(fileName);
				}
				File.WriteAllLines(saveFileDialog.FileName, messages);
			}
		}

		private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
		{
			messages.Clear();
			buttonClearLogs.IsEnabled = false;
			buttonExportLogs.IsEnabled = false;
		}
    }
}
