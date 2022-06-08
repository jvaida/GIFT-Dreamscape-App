using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Artanim.Experience
{
	public delegate void ValueUpdatedHandler(string key, object value);

	public interface IGameSession
	{
		event ValueUpdatedHandler OnValueUpdated;

		T GetValue<T>(string key, T defaultValue, string playerId = null);

		void SetValue(string key, object value, string playerId = null);

	}
}
