/*
	Heap.cs
	Author: Adrian Huang
	FOR RESEARCH OR STUDY ONLY, DO NOT USE THE CODE FOR COMMERCIAL

	I'll appreciate you posting question and bug to me
	email: sanyuexx@hotmail.com
	web	  :	http://www.cnblogs.com/dah
*/

namespace Adrian.Framework.DataStructure
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Collections;

	public interface ICloneable
	{
		object Clone();
	}

	public class Heap<T> : ICollection<T>, ICloneable where T : IComparable
	{
		#region Field

		private T[] _storage;
		private int _count;

		#endregion

		#region Constructor

		public Heap()
		{
			_storage = new T[1];
		}

		public Heap(IList<T> items)
		{
			_storage = new T[items.Count + 1];
			Count = items.Count;
			for (int i = 0; i < items.Count; i++)
			{
				_storage[i + 1] = items[i];
			}
			BuildHeap();
		}

		public Heap(IEnumerable<T> items)
			: this()
		{
			foreach (T item in items)
			{
				Insert(item);
			}
		}

		#endregion

		#region Public Method

		public void Insert(T item)
		{
			if (_storage.Length - Count <= 1)
			{
				EnsureCability(_storage.Length << 1);
			}
			int i;
			for (i = Count + 1; i >> 1 > 0 && _storage[i >> 1].CompareTo(item) > 0; i = i >> 1)
			{
				_storage[i] = _storage[i >> 1];
			}
			_storage[i] = item;
			Count++;
		}

		public T DeleteMin()
		{
			T min = _storage[1];
			_storage[1] = _storage[Count];
			Count--;
			PercolateDown(1);
			return min;
		}

		public T GetMin()
		{
			return _storage[1];
		}

		public bool Delete(T item)
		{
			int index = IndexOf(item);
			if (index > 0)
			{
				_storage[index] = _storage[Count];
				Count--;
				for (int i = index >> 1; i > 0; i = i >> 1)
				{
					PercolateDown(i);
				}
				return true;
			}
			return false;
		}

		public void BuildHeap()
		{
			for (int i = Count >> 1; i > 0; i--)
			{
				PercolateDown(i);
			}
		}

		public IList<T> ToSortedList()
		{
			List<T> sortedList = new List<T>();
			Heap<T> newHeap = (Heap<T>)Clone();
			while (newHeap.Count > 0)
			{
				sortedList.Add(newHeap.DeleteMin());
			}
			return sortedList;
		}

		public override string ToString()
		{
			if (Count > 0)
			{
				StringBuilder sb = new StringBuilder("{");
				foreach (T item in this)
				{
					sb.Append(item.ToString() + ",");
				}
				sb.Remove(sb.Length - 1, 1);
				sb.Append("}");
				return sb.ToString();
			}
			return "{ }";
		}

		#endregion

		#region Private Method

		private int IndexOf(T item)
		{
			for (int i = 1; i <= Count; i++)
			{
				try
				{
					if (_storage[i].CompareTo(item) == 0)
						return i;
				}
				catch (Exception) { }
			}
			return -1;
		}

		private void PercolateDown(int index)
		{
			int i;
			int child;
			T temp = _storage[index];
			for (i = index; i << 1 <= Count; i = child)
			{
				child = i << 1;
				if (child != Count && _storage[child].CompareTo(_storage[child + 1]) > 0)
				{
					child++;
				}
				if (temp.CompareTo(_storage[child]) > 0)
					_storage[i] = _storage[child];
				else
					break;
			}
			_storage[i] = temp;
		}

		private void EnsureCability(int cability)
		{
			T[] newStorage = new T[cability];
			Array.Copy(_storage, newStorage, _storage.Length);
			_storage = newStorage;
		}

		#endregion

		#region Property

		public int Count
		{
			get { return _count; }
			private set { _count = value; }
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			Insert(item);
		}

		public void Clear()
		{
			_storage = new T[1];
			Count = 0;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) > 0;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(_storage, 1, array, arrayIndex, Count);
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return Delete(item);
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 1; i <= Count; i++)
			{
				yield return _storage[i];
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}

		#endregion

		#region ICloneable Members

		public object Clone()
		{
			Heap<T> newHeap = new Heap<T>();
			newHeap._storage = (T[])_storage.Clone();
			newHeap._count = _count;
			return newHeap;
		}

		#endregion
	}
}
