﻿using System;
using System.Threading;
using Storm.Mvvm.Inject;

namespace Storm.Mvvm.Services
{
	public class DispatcherService : AbstractActivityUpdatable, IDispatcherService
	{
		public void InvokeOnUIThread(Action action)
		{
			if (SynchronizationContext.Current != null)
			{
				action();
			}
			else
			{
				CurrentActivity.RunOnUiThread(action);
			}
		}

		public void InvokeOnUIThread<T>(Func<T> action, Action<T> callback)
		{
			Action execution = () =>
			{
				T result = action();
				callback(result);
			};
		
			if (SynchronizationContext.Current != null)
			{
				execution();
			}
			else
			{
				CurrentActivity.RunOnUiThread(execution);
			}
		}
	}
}
