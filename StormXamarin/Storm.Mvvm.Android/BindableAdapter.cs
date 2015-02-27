﻿using System.Collections;
using System.Collections.Specialized;
using Android.Views;
using Android.Widget;
using Storm.Mvvm.Interfaces;

namespace Storm.Mvvm
{
	public class BindableAdapter : BaseAdapter<object>, IMvvmAdapter, ISearchableAdapter
	{
		private ITemplateSelector _templateSelector;
		private IList _collection;

		public object Collection
		{
			get { return _collection; }
			set
			{
				if (!object.Equals(_collection, value))
				{
					Unregister(_collection);
					_collection = value as IList;
					Register(_collection);
					NotifyDataChanged();
				}
			}
		}

		public ITemplateSelector TemplateSelector
		{
			get { return _templateSelector; }
			set
			{
				if (!object.Equals(_templateSelector, value))
				{
					_templateSelector = value;
					NotifyDataChanged();
				}
			}
		}

		public override object this[int position]
		{
			get { return _collection == null ? null : _collection[position]; }
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			return _templateSelector.GetView(this[position], parent, convertView);
		}

		public override int Count
		{
			get
			{
				return _collection == null ? 0 : _collection.Count;
			}
		}

		public int IndexOf(object value)
		{
			return _collection == null ? -1 : _collection.IndexOf(value);
		}

		private void Unregister(object collection)
		{
			INotifyCollectionChanged observable = collection as INotifyCollectionChanged;
			if (observable != null)
			{
				observable.CollectionChanged -= OnCollectionChanged;
			}
		}

		private void Register(object collection)
		{
			INotifyCollectionChanged observable = collection as INotifyCollectionChanged;
			if (observable != null)
			{
				observable.CollectionChanged += OnCollectionChanged;
			}
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			NotifyDataChanged();
		}

		private void NotifyDataChanged()
		{
			if (_templateSelector != null)
			{
				NotifyDataSetChanged();
			}
		}
	}
}
