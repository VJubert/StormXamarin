﻿using System.Collections.Generic;

namespace Storm.Mvvm
{
	public interface INavigationService
	{
		bool CanGoBack { get; }

		bool CanGoForward { get; }

		void Navigate(string _view);

		void Navigate(string _view, Dictionary<string, object> _parameters);

		void NavigateAndReplace(string _view);

		void NavigateAndReplace(string _view, Dictionary<string, object> _parameters);

		void GoBack();

		void GoForward();
	}
}
