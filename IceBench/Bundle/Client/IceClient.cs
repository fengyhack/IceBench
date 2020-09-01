using Ice;
using System;
using System.Threading;
using System.Collections.Generic;

namespace Bundle
{
	public class IceClient
	{
		public event StatusChangedNotify OnStatusChanged;

		public event MethodInvokedNotify OnMethodInvoked;

		public event ExceptionOccurredNotify OnExceptionOccured;

		private Communicator communicator;

		private bool isAsyncMode;

		private int contentSizeMB;

		private bool isRunning;

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

		public IceClient(bool isAsync, int sizeMB)
		{
			isAsyncMode = isAsync;
			contentSizeMB = sizeMB;
			isRunning = false;
			counter = 0;
			rand = new Random();
		}

		public void SetAsync(bool isAsync)
        {
			isAsyncMode = isAsync;
        }

		public void SetContentSize(int sizeMB)
		{
			if (sizeMB > 0)
			{
				contentSizeMB = sizeMB;
			}
		}

		public async void Start(string args)
		{
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
					communicator = Util.initialize(initData);
					WorkerPrx workerPrx = WorkerPrxHelper.checkedCast(communicator.stringToProxy("SimpleMessenger:" + args));
					if (workerPrx == null)
					{
						throw new ApplicationException("Invalid Proxy");
					}
					isRunning = true;
					Status = BundleStatus.Running;
					while (isRunning && communicator != null)
					{
						var operation = operations[counter % operations.Count];
						++counter;
						OnMethodInvoked?.Invoke(operation, isAsyncMode);
						if (isAsyncMode)
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

		public void Stop()
		{
			isRunning = false;
			if (communicator != null)
			{
				Status = BundleStatus.Stopping;
				try
				{
					communicator.shutdown();
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
