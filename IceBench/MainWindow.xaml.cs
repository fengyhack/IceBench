using Bundle;
using Ice;
using IceInternal;
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

		private bool isInvoking;

		private int contentSizeMB;

		private readonly List<string> messages;

		public MainWindow()
		{
			InitializeComponent();
			messages = new List<string>();
			isInvoking = false;
			textBoxServerArgs.Text = "SimpleMessenger: default -h localhost -p 5050 -t 5000";
			buttonStartServer.IsEnabled = true;
			buttonStopServer.IsEnabled = false;
			buttonRestartServer.IsEnabled = false;
			textBoxClientArgs.Text = "SimpleMessenger: default -h localhost -p 5050 -t 2000";
			buttonStartClient.IsEnabled = true;
			buttonIdleClient.IsEnabled = false;
			buttonStopClient.IsEnabled = false;
            buttonInvokeCall.IsEnabled = false;
            buttonRestartClient.IsEnabled = false;
			textBoxServerRestartInterval.Text = "1000";
			textBoxClientRestartInterval.Text = "1000";
			textBoxContentSize.Text = "2";
			checkBoxAsync.IsChecked = false;
			buttonClearLogs.IsEnabled = false;
			buttonExportLogs.IsEnabled = false;
			textBoxContentSize.IsEnabled = false;
			iceServer = new IceServer();
			iceClient = new IceClient();
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
			if (status != BundleStatus.Running)
			{
				App.Current.Dispatcher.Invoke(() =>
				{
					tbTime.Text = "";
				});
				timer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
			System.Windows.Application.Current.Dispatcher.Invoke(delegate
			{
				labelServerStatus.Content = $"{status}";
				labelServerStatus.Foreground = Brushes.SteelBlue;
				cbServerClose.IsEnabled = (status != BundleStatus.Running);
				cbServerHeartbeat.IsEnabled = (status != BundleStatus.Running);
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
			AddMessage("Server Executing: " + operation + ", AMI: " + isAsync);
		}

		private void IceServer_OnExceptionOccured(System.Exception exception)
		{
			AddMessage($"Server Exception: {exception.Message}");
		}

		private void IceClient_OnStatusChanged(BundleStatus status)
		{
			AddMessage($"Client Status: {status}");
			if (status != BundleStatus.Running)
			{
				App.Current.Dispatcher.Invoke(() =>
				{
					tbTime.Text = "";
				});
				timer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
			System.Windows.Application.Current.Dispatcher.Invoke(delegate
			{
				if (status != BundleStatus.Unknown)
				{
					textBoxContentSize.IsEnabled = true;
				}
				labelClientStatus.Content = $"{status}";
				labelClientStatus.Foreground = Brushes.SteelBlue;
				cbClientHeartbeat.IsEnabled = (status != BundleStatus.Running);
				checkBoxAsync.IsEnabled = (status == BundleStatus.Running);
				buttonIdleClient.IsEnabled = (status == BundleStatus.Running);
				buttonIdleClient.Content = iceClient.Hold ? "Run" : "Idle";
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

		private int counter = 0;
		private System.Threading.Timer timer = null;

		private void TimerCallback(object state)
        {
			++counter;
			App.Current.Dispatcher.Invoke(() =>
			{
				tbTime.Text = $"{counter}";
			});
			timer.Change(1000, 1000);
		}

		private void IceClient_OnMethodInvoked(OperationType operation, bool isAsync)
		{
			AddMessage("Client Calling:   " + operation + ", IsAsync: " + isAsync);
			counter = 0;
			App.Current.Dispatcher.Invoke(() =>
			{
				tbTime.Text = $"{counter}";
			});
			if (timer == null)
			{
				timer = new System.Threading.Timer(TimerCallback);
			}
			timer.Change(1000, 1000);
		}

		private void IceClient_OnExceptionOccured(System.Exception exception)
		{
			AddMessage($"Client Exception: {exception.Message}");
			timer.Change(Timeout.Infinite, Timeout.Infinite);
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

			if(timer != null)
            {
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				timer.Dispose();
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
			var acmClose = Bundle.ACMCloseFlag.CloseOff;
			var acmHeartbeat = Bundle.ACMHeartbeatFlag.HeartbeatOff;
			App.Current.Dispatcher.Invoke(() =>
			{
				Enum.TryParse("Close" + cbServerClose.Text, out acmClose);
				Enum.TryParse("Heartbeat" + cbServerHeartbeat.Text, out acmHeartbeat);
			});
			Thread.Sleep(1);
			Task.Run(() =>
			{
				iceServer.Start(serverArgs, acmClose, acmHeartbeat);
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
            buttonInvokeCall.IsEnabled = false;
            AddMessage("User Restart Server: " + serverArgs);
			Task.Run(delegate
			{
				iceServer.OnStatusChanged -= IceServer_OnStatusChanged;
				iceServer.OnExceptionOccured -= IceServer_OnExceptionOccured;
				iceServer.OnMethodInvoked -= IceServer_OnMethodInvoked;
				iceServer.Exit();
				Thread.Sleep(serverRestartIntervalMS);
				iceServer = new IceServer();
				iceServer.OnStatusChanged += IceServer_OnStatusChanged;
				iceServer.OnExceptionOccured += IceServer_OnExceptionOccured;
				iceServer.OnMethodInvoked += IceServer_OnMethodInvoked;
				System.Windows.Application.Current.Dispatcher.Invoke(delegate
				{
					buttonStopServer.IsEnabled = true;
					buttonRestartServer.IsEnabled = true;
					buttonInvokeCall.IsEnabled = true;
				});
				var acmClose = Bundle.ACMCloseFlag.CloseOff;
				var acmHeartbeat = Bundle.ACMHeartbeatFlag.HeartbeatOff;
                App.Current.Dispatcher.Invoke(() =>
                {
                    Enum.TryParse("Close" + cbServerClose.Text, out acmClose);
                    Enum.TryParse("Heartbeat" + cbServerHeartbeat.Text, out acmHeartbeat);
                });
                Thread.Sleep(1);
				Task.Run(() =>
				{
					iceServer.Start(serverArgs, acmClose, acmHeartbeat);
				});
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
            buttonInvokeCall.IsEnabled = true;
            AddMessage("User Start Client: " + clientArgs);
			var acmHeartbeat = Bundle.ACMHeartbeatFlag.HeartbeatOff;
			var amiEnabled = false;
			App.Current.Dispatcher.Invoke(() =>
			{
				Enum.TryParse("Heartbeat" + cbClientHeartbeat.Text, out acmHeartbeat);
				amiEnabled = checkBoxAsync.IsChecked.Value;
			});
			Thread.Sleep(1);
			iceClient.SetContentSize(contentSizeMB);
			Task.Run(() =>
			{
				iceClient.Start(clientArgs, true, amiEnabled, acmHeartbeat);
			});
		}

		private void BtnStopClient_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartClient.IsEnabled = true;
			buttonStartClient.IsEnabled = true;
			buttonStopClient.IsEnabled = false;
            buttonInvokeCall.IsEnabled = false;
            AddMessage("User Stop Client");
			iceClient.Stop();
		}

		private void BtnRestartClient_Click(object sender, RoutedEventArgs e)
		{
			buttonRestartClient.IsEnabled = false;
			buttonStartClient.IsEnabled = false;
			buttonStopClient.IsEnabled = false;
			buttonInvokeCall.IsEnabled = false;
            AddMessage("User Restart Client: " + clientArgs);
			Task.Run(delegate
			{
				iceClient.OnStatusChanged -= IceClient_OnStatusChanged;
				iceClient.OnExceptionOccured -= IceClient_OnExceptionOccured;
				iceClient.OnMethodInvoked -= IceClient_OnMethodInvoked;
				iceClient.Exit();
				IceClient_OnStatusChanged(iceClient.Status);
				Thread.Sleep(clientRestartIntervalMS);
				iceClient = new IceClient();
				iceClient.OnStatusChanged += IceClient_OnStatusChanged;
				iceClient.OnExceptionOccured += IceClient_OnExceptionOccured;
				iceClient.OnMethodInvoked += IceClient_OnMethodInvoked;
				System.Windows.Application.Current.Dispatcher.Invoke(delegate
				{	
					buttonStopClient.IsEnabled = true;
					buttonRestartClient.IsEnabled = true;
                    buttonInvokeCall.IsEnabled = true;
                });
				IceClient_OnStatusChanged(iceClient.Status);
				var acmHeartbeat = Bundle.ACMHeartbeatFlag.HeartbeatOff;
				var amiEnabled = false;
				App.Current.Dispatcher.Invoke(() =>
				{
					Enum.TryParse("Heartbeat" + cbClientHeartbeat.Text, out acmHeartbeat);
					amiEnabled = checkBoxAsync.IsChecked.Value;
				});
				Thread.Sleep(1);
				iceClient.SetContentSize(contentSizeMB);
				var hold = iceClient.Hold;
				Task.Run(() =>
				{
					iceClient.Start(clientArgs, hold, amiEnabled, acmHeartbeat);
				});
			});
		}

		private void CheckBoxAsync_Checked(object sender, RoutedEventArgs e)
		{
			isAsyncMode = true;
			iceClient.SetAMI(isAsyncMode);
		}

		private void CheckBoxAsync_Unchecked(object sender, RoutedEventArgs e)
		{
			isAsyncMode = false;
			iceClient.SetAMI(isAsyncMode);
		}

		private void AddMessage(string message)
		{
			string str = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] {message}";
			messages.Add(str);
			System.Windows.Application.Current.Dispatcher.Invoke(delegate
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

        private void BtnIdleClient_Click(object sender, RoutedEventArgs e)
        {
			iceClient.ToggleHold();
			if (iceClient.Hold)
			{
				buttonIdleClient.Content = "Run";
			}
			else
			{
				buttonIdleClient.Content = "Idle";

			}
		}

        private void BtnInvokeCall_Click(object sender, RoutedEventArgs e)
        {
			if(isInvoking)
			{
				return;
			}
			Task.Run(() =>
			{
				try
				{
                    iceClient.InvokeCall();
                }
				catch(System.Exception ex)
				{
                    AddMessage($"Invoke Call Exception: {ex.Message}");
				}
				finally
				{
					isInvoking = false;
                }
            });
        }
    }
}
