using Ice;
using System;
using System.Threading;
using System.Collections.Generic;
using IceBench.Bundle;

namespace Bundle
{
	public class IceClient
	{
		public bool Hold { get; private set; }

		public event StatusChangedNotify OnStatusChanged;

		public event MethodInvokedNotify OnMethodInvoked;

		public event ExceptionOccurredNotify OnExceptionOccured;

		private Communicator communicator;

		private int contentSizeMB;

		private bool isRunning;

		private bool amiEnabled;

		private ACMHeartbeatFlag acmHeartbeat;

		private int counter;

		private readonly Random rand;

		private BundleStatus _Status;
		public BundleStatus Status
		{
			get
			{
				return _Status;
			}
			private set
			{
				if (_Status != value)
				{
					_Status = value;
					OnStatusChanged?.Invoke(_Status);
				}
			}
		}

		private readonly List<OperationType> operations = new List<OperationType>
		{
			OperationType.SmallFile,
			OperationType.BigFile,
			OperationType.LongTime
		};

		public IceClient()
		{
			Hold = false;
			amiEnabled = false;
			acmHeartbeat = ACMHeartbeatFlag.HeartbeatOff;
			contentSizeMB = 2;
			isRunning = false;
			counter = 0;
			rand = new Random();
		}

		public void SetAMI(bool enabled)
        {
			amiEnabled = enabled;
        }

		public void SetContentSize(int sizeMB)
		{
			if (sizeMB > 0)
			{
				contentSizeMB = sizeMB;
			}
		}

		public async void Start(string args, bool hold = false, bool ami = false, ACMHeartbeatFlag heartbeat = ACMHeartbeatFlag.HeartbeatOff)
		{
			Hold = hold;
			amiEnabled = ami;
			acmHeartbeat = heartbeat;
			if (communicator == null || communicator.isShutdown())
			{
				Status = BundleStatus.Starting;
				try
				{
					const int SIZE_MB = 128;
					const int SIZE_MAX = SIZE_MB * 1024 * 1024;
					if (contentSizeMB < 0 || contentSizeMB > SIZE_MB)
					{
						contentSizeMB = 1;
					}
					var initData = new InitializationData();
					initData.properties = Util.createProperties();
					initData.properties.setProperty("Ice.MessageSizeMax", $"{SIZE_MAX}");
					initData.properties.setProperty("Filesystem.MaxFileSize", $"{SIZE_MAX}");
					initData.properties.setProperty("Ice.ACM.Heartbeat", $"{(int)acmHeartbeat}");
					communicator = Util.initialize(initData);
					WorkerPrx workerPrx = WorkerPrxHelper.checkedCast(communicator.stringToProxy(args));
					if (workerPrx == null)
					{
						throw new ApplicationException("Invalid Proxy");
					}
					isRunning = true;
					Status = BundleStatus.Running;
					while (isRunning && communicator != null)
					{
						if(Hold)
                        {
							Thread.Sleep(100);
							continue;
                        }

						var operation = operations[counter % operations.Count];
						++counter;
						OnMethodInvoked?.Invoke(operation, amiEnabled);
						if (amiEnabled)
						{
							try
							{
								var result = workerPrx?.PerformActionEx(operation, contentSizeMB);
							}
							catch(OperationException ex)
                            {
								OnExceptionOccured?.Invoke(ex);
							}
							Thread.Sleep(rand.Next(200, 1000));
						}
						else
						{
							try
							{
								var result = workerPrx?.PerformAction(operation, contentSizeMB);
							}
							catch (OperationException ex)
							{
								OnExceptionOccured?.Invoke(ex);
							}
							Thread.Sleep(500);
						}
					}
					Status = BundleStatus.Idle;
				}
				catch (System.Exception ex)
				{
					Status = BundleStatus.Exception;
					OnExceptionOccured?.Invoke(ex);
				}
			}
			else
			{
				isRunning = false;
				Status = BundleStatus.Unknown;
			}
		}

		public void ToggleHold()
        {
			Hold = !Hold;
        }

		public void SetHeartbeat(ACMHeartbeatFlag heartbeat)
        {
			acmHeartbeat = heartbeat;
        }

		public void Stop()
		{
			isRunning = false;
			if (communicator != null)
			{
				Status = BundleStatus.Stopping;
				try
				{
					communicator.shutdown();
					communicator.waitForShutdown();
				}
				catch (System.Exception ex)
				{
					Status = BundleStatus.Exception;
					OnExceptionOccured?.Invoke(ex);
				}
				Status = BundleStatus.Stopped;
			}
			else
			{
				Status = BundleStatus.Unknown;
			}
		}

		public void Exit()
		{
			isRunning = false;
			if (communicator != null)
			{
				Status = BundleStatus.Exiting;
				try
				{
					communicator.shutdown();
					communicator.destroy();
					Status = BundleStatus.Shutdown;
				}
				catch (System.Exception ex)
				{
					Status = BundleStatus.Exception;
					OnExceptionOccured?.Invoke(ex);
				}
				communicator = null;
			}
			else
			{
				Status = BundleStatus.Unknown;
			}
			counter = 0;
		}
	}
}
