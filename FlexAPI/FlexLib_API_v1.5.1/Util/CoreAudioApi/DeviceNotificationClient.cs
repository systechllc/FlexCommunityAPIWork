using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    public class DeviceNotificationClient : IMMNotificationClient
    {
        public delegate void DefaultDeviceChangedEventHandler(DataFlow flow, Role role, string pwstrDefaultDevice);
        public event DefaultDeviceChangedEventHandler DefaultDeviceChanged;

        public delegate void DeviceAddedEventHandler(string pwstrDeviceId);
        public event DeviceAddedEventHandler DeviceAdded;

        public delegate void DeviceRemovedEventHandler(string pwstrDeviceId);
        public event DeviceRemovedEventHandler DeviceRemoved;

        public delegate void DeviceStateChangedEventHandler(string pwstrDeviceId, DeviceState dwNewState);
        public event DeviceStateChangedEventHandler DeviceStateChanged;

        public delegate void PropertyValueChangedEventHandler(string pwstrDeviceId, PropertyKey key);
        public event PropertyValueChangedEventHandler PropertyValueChanged;


        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            if (DeviceStateChanged != null)
            {
                DeviceStateChanged(deviceId, newState);
            }
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId)
        {
            if (DeviceAdded != null)
            {
                DeviceAdded(pwstrDeviceId);
            }
        }

        void IMMNotificationClient.OnDeviceRemoved(string deviceId)
        {
            if (DeviceRemoved != null)
            {
                DeviceRemoved(deviceId);
            }
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            try
            {
                if (DefaultDeviceChanged != null)
                {
                    DefaultDeviceChanged(flow, role, defaultDeviceId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.ToString());
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            if (PropertyValueChanged != null)
            {
                PropertyValueChanged(pwstrDeviceId, key);
            }
        }


    }
}