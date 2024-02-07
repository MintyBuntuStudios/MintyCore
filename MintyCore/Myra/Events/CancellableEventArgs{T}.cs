using System;

namespace MintyCore.Myra.Events
{
	public class CancellableEventArgs<T> : EventArgs
	{
		public T Data { get; private set; }
		public bool Cancel { get; set; }

		public CancellableEventArgs(T data)
		{
			Data = data;
		}
	}
}