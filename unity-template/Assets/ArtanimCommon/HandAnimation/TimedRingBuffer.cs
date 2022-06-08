using System;

namespace Artanim.HandAnimation
{
	/// <summary>
	/// A simple ringbuffer of time-based data, to be used when we're looking to interpolate between two datasets for a given time
	/// </summary>
	/// <typeparam name="T">The datatype to store</typeparam>
	public class TimedRingBuffer<T>
	{
		private int _BeginIndex;
		private int _EndIndex;

		private int _BufferSize;

		public int BufferCount
		{
			get
			{
				return _BufferCount;
			}
		}
		private int _BufferCount;

		private Tuple<float, T>[] _Buffer;

		public TimedRingBuffer(int buffer_size)
		{
			_BufferSize = buffer_size;
			_Buffer = new Tuple<float, T>[_BufferSize];
		}

		public void Add(float time_stamp, T item)
		{
			int index;

			if (_BufferCount < _BufferSize)
			{
				index = _BufferCount;
				_EndIndex = _BufferCount;
				_BufferCount += 1;
			}
			else
			{
				index = (_EndIndex + 1) % _BufferSize;
				_EndIndex = index;
				_BeginIndex = (_BeginIndex + 1) % _BufferSize;
			}

			_Buffer[index] = new Tuple<float, T>(time_stamp, item);
		}

		public bool GetItemsAroundTime(float t, out Tuple<float, T> before, out Tuple<float, T> after)
		{
			Tuple<float, T> current_before = new Tuple<float, T>(0.0f, default(T));
			Tuple<float, T> current_after = new Tuple<float, T>(0.0f, default(T));

			bool found_before = false;
			bool found_after = false;

			for (int i = 0; i < _BufferCount; ++i)
			{
				int index = (_BeginIndex + i) % _BufferSize;

				if (_Buffer[index].Item1 < t)
				{
					current_before = _Buffer[index];
					found_before = true;
				}
				if (_Buffer[index].Item1 >= t)
				{
					current_after = _Buffer[index];
					found_after = true;
					break;
				}
			}

			before = current_before;
			after = current_after;
			return (found_before && found_after);
		}

		public void Clear()
		{
			_BeginIndex = 0;
			_EndIndex = 0;
			_BufferCount = 0;
		}
	}
}