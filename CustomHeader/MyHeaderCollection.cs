using System;
using System.Collections;

namespace CustomHeader
{
	public class MyHeaderCollection : CollectionBase
	{
		public event EventHandler ColumnAdded;
		public event EventHandler ColumnRemoved;
		public MyColumn Add(MyColumn value)
		{
			base.List.Add(value as object);
			if(ColumnAdded != null)
				ColumnAdded(value, new EventArgs());
			return value;
		}

		public void AddRange(MyColumn[] values)
		{
			foreach(MyColumn ip in values)
				Add(ip);
		}

		public void Remove(MyColumn value)
		{
			base.List.Remove(value as object);
			if(ColumnRemoved != null)
				ColumnRemoved(value, new EventArgs());
		}

		public void Insert(int index, MyColumn value)
		{
			base.List.Insert(index, value as object);
			if(ColumnAdded != null)
				ColumnAdded(this, new EventArgs());
		}

		public bool Contains(MyColumn value)
		{
			return base.List.Contains(value as object);
		}

		public MyColumn this[int index]
		{
			get { return (base.List[index] as MyColumn); }
		}

		public int IndexOf(MyColumn value)
		{
			return base.List.IndexOf(value);
		}
	}
}
