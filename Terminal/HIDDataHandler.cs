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

        private CurrentPacketParser _parser;

        public HIDDataHandler(CurrentPacketParser parser)
        {
            _parser = parser;
        }

        public IEnumerable<string> ListAvailableDevices()
        {
            return HidDevices.Enumerate().Select(GetFriendlyName);
        }

        private string GetFriendlyName(HidDevice device)
        {
            return string.Format("{0}, {1}: {2}", device.Attributes.VendorHexId, device.Attributes.ProductHexId, device.Description);
        }

        public string[] Devices
        {
            get { return HidDevices.Enumerate().Select(x => x.Description).ToArray(); }
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
            get
            {
                return deviceType.HID;
            }
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
                //if (_device == null)
                //    return;
                if (_device == null)
                    throw new ArgumentException("_device should not be null");

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
                    // todo: Do this in a separate thread
                    _device.CloseDevice();
                    _device.MonitorDeviceEvents = false;
                    _device.Removed -= DeviceOnRemoved;
                }
            }
        }

        private void ReadReportCallback(HidReport report)
        {
            Log.Debug("Got a ReadReportCallback of length {0}", report.Data.Length);

            _device.ReadReport(ReadReportCallback);

            if (DataReceived == null)
                return;

            var data = report.GetBytes();
            var dataString = _parser.CurrentParser.InterpretPacket(data);
            var args = new DataReceivedEventArgs(dataString);
            DataReceived(this, args);
        }

        private void ReadCallback(HidDeviceData data)
        {
            Log.Debug("Got a ReadCallback of length {0}", data.Data.Length);

            _device.Read(ReadCallback);

            if (DataReceived == null)
                return;

            var bytes = data.Data;
            var dataString = _parser.CurrentParser.InterpretPacket(bytes);
            var args = new DataReceivedEventArgs(dataString);
            DataReceived(this, args);
        }

        private void DeviceOnRemoved()
        {
            throw new NotImplementedException();
        }

        public int Write(byte[] data)
        {
            //var header = new byte[] { 0x01, 0xF0, 0x10, 0x03, 0xA0, 0x01, 0x0F, 0x58, 0x04 };
            var header = data;
            var writeReport = _device.CreateReport();
            int i = 0;
            if (writeReport.Data.Length > 0)
                foreach (var b in header)
                    writeReport.Data[i++] = b;
            _device.WriteReport(writeReport);
            return data.Length;
        }

        public int Write(byte data)
        {
            return Write(new[] { data });
        }

        public int Write(string data)
        {
            return Write(Encoding.UTF8.GetBytes(data));
        }

        public int Write(char data)
        {
            return Write(new[] { (byte)data });
        }

        public event DataReceivedEventHandler DataReceived;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}