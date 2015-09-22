﻿using System;
using System.Collections;
using System.Collections.Specialized;
using Android.Views;
using Android.Widget;

namespace Storm.MvvmCross.Android.Adapters
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
				if (!Equals(_collection, value))
				{
					Unregister(_collection);
					_collection = value as IList;
					if (_collection == null)
					{
						IList list = value as IList;
						IEnumerable enumerable = value as IEnumerable;
						if (list != null)
						{
							_collection = list;
						}
						else if (enumerable != null)
						{
							_collection = new EnumerableCursor(enumerable);
						}
						else if (value != null)
						{
							throw new InvalidOperationException("Binding with adapter only support collection binding which implement IEnumerable");
						}
					}
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
				if (!Equals(_templateSelector, value))
				{
					_templateSelector = value;
					NotifyDataChanged();
				}
			}
		}

		public override object this[int position]
		{
			get
			{
				if (_collection == null || position < 0 || position >= _collection.Count)
				{
					return null;
				}
				return _collection[position];
			}
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			return TemplateSelector.GetView(this[position], parent, convertView);
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

		private void Register(object collection)
		{
			INotifyCollectionChanged observable = collection as INotifyCollectionChanged;
			if (observable != null)
			{
				observable.CollectionChanged += OnCollectionChanged;
			}
		}

		private void Unregister(object collection)
		{
			INotifyCollectionChanged observable = collection as INotifyCollectionChanged;
			if (observable != null)
			{
				observable.CollectionChanged -= OnCollectionChanged;
			}
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			NotifyDataChanged();
		}

		private void NotifyDataChanged()
		{
			if (TemplateSelector != null)
			{
				NotifyDataSetChanged();
			}
		}
	}
}
