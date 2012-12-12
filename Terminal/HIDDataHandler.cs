﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Anotar;
using HidLibrary;
using NLog;
using Terminal_Interface;
using Terminal_Interface.Enums;
using Terminal_Interface.Events;

namespace Terminal
{
    public class HIDDataHandler : IDataDevice
    {
        private HidDevice _device;

        public IEnumerable<string> ListAvailableDevices()
        {
            var devices = HidDevices.EnumerateHidDeviceInstances();
            List<string> names = new List<string>();
            foreach (HidDevice device in devices)
            {
                names.Add(GetFriendlyName(device));
            }
            return names.AsEnumerable();
        }

        private string GetFriendlyName(HidDevice device)
        {
            return string.Format("{0}, {1}: {2}", device.Attributes.VendorHexId, device.Attributes.ProductHexId, device.Attributes.BusReportedDescription);
        }

        public string[] Devices
        {
            get { return HidDevices.EnumerateHidDevices().ToArray(); }
        }

        public string DeviceName
        {
            get
            {
                if (_device == null)
                    return string.Empty;

                return _device.DevicePath;
            }
            set
            {
                _device = HidDevices.GetDevice(value);
            }
        }

        public string FriendlyName
        {
            get { return string.Format("HID: {0}", GetFriendlyName(_device)); }
        }

        public string DeviceStatus
        {
            get { throw new NotImplementedException(); }
        }

        public deviceType DeviceType
        {
            get { throw new NotImplementedException(); }
        }

        public PortState PortState
        {
            get
            {
                if (_device == null)
                    return PortState.Error;

                return _device.IsOpen ? PortState.Open : PortState.Closed;
            }
            set
            {
                if (_device == null)
                    return;

                if (value == PortState.Open)
                {
                    _device.OpenDevice();
                    _device.MonitorDeviceEvents = true;
                    _device.Removed += DeviceOnRemoved;
                    _device.Read(ReadCallback);
                    _device.ReadReport(ReadReportCallback);
                }
                else
                {
                    _device.CloseDevice();
                    _device.MonitorDeviceEvents = false;
                    _device.Removed -= DeviceOnRemoved;
                }
            }
        }

        private void ReadReportCallback(HidReport report)
        {
            _device.ReadReport(ReadReportCallback);

            if (DataReceived == null)
                return;

            var data = report.GetBytes();
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2},", b);

            var args = new DataReceivedEventArgs(hex.ToString());
            DataReceived(this, args);
        }

        private void ReadCallback(HidDeviceData data)
        {
            _device.Read(ReadCallback);

            if (DataReceived == null)
                return;

            StringBuilder hex = new StringBuilder(data.Data.Length * 2);
            foreach (byte b in data.Data)
                hex.AppendFormat("{0:x2},", b);

            var args = new DataReceivedEventArgs(hex.ToString());
            DataReceived(this, args);
        }

        private void DeviceOnRemoved()
        {
            throw new NotImplementedException();
        }

        public void KeyPressed(char c)
        {
            throw new NotImplementedException();
        }

        public int Write(byte[] data)
        {
            _device.Write(data);
            return data.Length;
        }

        public int Write(byte data)
        {
            _device.Write(new[] { data });
            return 1;
        }

        public int Write(string data)
        {
            throw new NotImplementedException();
        }

        public int Write(char data)
        {
            _device.Write(new[] { (byte)data });
            return 1;
        }

        public event DataReceivedEventHandler DataReceived;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}