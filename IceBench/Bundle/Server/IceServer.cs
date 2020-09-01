using Ice;
using System.Threading.Tasks;

namespace Bundle
{
	public class IceServer
	{
		public event StatusChangedNotify OnStatusChanged;

		public event MethodInvokedNotify OnMethodInvoked;

		public event ExceptionOccurredNotify OnExceptionOccured;

		private Communicator communicator;

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

		private WorkerImpl servant;

		public void Start(string args)
		{
			if (communicator == null || communicator.isShutdown())
			{
				Status = BundleStatus.Starting;
				try
				{
					const int SIZE_MAX = 128 * 1024 * 1024;
					var initData = new InitializationData();
					initData.properties = Util.createProperties();
					initData.properties.setProperty("Ice.MessageSizeMax", $"{SIZE_MAX}");
					initData.properties.setProperty("Filesystem.MaxFileSize", $"{SIZE_MAX}");
					communicator = Util.initialize(initData);
					ObjectAdapter objectAdapter = communicator.createObjectAdapterWithEndpoints("SimpleMessengerAdapter", args);
					servant = new WorkerImpl(OnMethodInvoked);
					Identity id = Util.stringToIdentity("SimpleMessenger");
					objectAdapter.add(servant, id);
					objectAdapter.activate();
					Status = BundleStatus.Running;
					communicator.waitForShutdown();
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
				Status = BundleStatus.Unknown;
			}
		}

		public void SetUserRequestError()
		{
			servant?.SetErrorFlag();
		}

		public void Stop()
		{
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
		}
	}
}
