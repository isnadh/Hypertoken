﻿using Terminal.Interface.Events;

namespace Terminal.Interface
{
	public delegate void SettingChangedEventHandler(object sender, SettingChangedEventArgs e);

	public interface ISettings
	{
		void Set(string key, object value);

		object Get(string key);

		event SettingChangedEventHandler SettingChanged;
	}
}