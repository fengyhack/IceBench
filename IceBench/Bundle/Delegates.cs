using System;

namespace Bundle
{
	public delegate void StatusChangedNotify(BundleStatus status);

	public delegate void MethodInvokedNotify(OperationType method, bool isAsync);

	public delegate void ExceptionOccurredNotify(Exception exception);
}
