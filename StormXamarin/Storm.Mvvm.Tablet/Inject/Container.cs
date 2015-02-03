﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Storm.Mvvm.Services;

namespace Storm.Mvvm.Inject
{
	public class Container : ContainerBase
	{
		private readonly Frame _rootFrame;

		public Container(Frame rootFrame)
		{
			_rootFrame = rootFrame;
		}

		public void Initialize(Dictionary<string, Type> views)
		{
			RegisterInstance<INavigationService>(new NavigationService(_rootFrame, views));
			RegisterInstance<IDispatcherService>(new DispatcherService());
			RegisterInstance<ILoggerService>(new LoggerService());
		}
	}
}
