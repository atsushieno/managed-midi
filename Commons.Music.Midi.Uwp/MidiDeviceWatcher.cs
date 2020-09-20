//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Commons.Music.Midi.Uwp
{
    /// <summary>
    /// DeviceWatcher class to monitor adding/removing MIDI devices on the fly
    /// </summary>
    internal class MidiDeviceWatcher
    {
        internal DeviceWatcher deviceWatcher = null;
        internal DeviceInformationCollection deviceInformationCollection = null;
        bool enumerationCompleted = false;
        string midiSelector = string.Empty;
        CoreDispatcher coreDispatcher = null;

        public bool IsEnumerated => enumerationCompleted;
        public ObservableCollection<IMidiPortDetails> DeviceCollection { get; }

        /// <summary>
        /// Constructor: Initialize and hook up Device Watcher events
        /// </summary>
        /// <param name="midiSelectorString">MIDI Device Selector</param>
        /// <param name="dispatcher">CoreDispatcher instance, to update UI thread</param>
        /// <param name="portListBox">The UI element to update with list of devices</param>
        internal MidiDeviceWatcher(string midiSelectorString, CoreDispatcher dispatcher)
        {
            DeviceCollection = new ObservableCollection<IMidiPortDetails>();

            this.deviceWatcher = DeviceInformation.CreateWatcher(midiSelectorString);
            this.midiSelector = midiSelectorString;
            this.coreDispatcher = dispatcher;

            this.deviceWatcher.Added += OnDeviceAdded;
            this.deviceWatcher.Removed += OnDeviceRemoved;
            this.deviceWatcher.Updated += OnDeviceUpdated;
            this.deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
        }

        /// <summary>
        /// Destructor: Remove Device Watcher events
        /// </summary>
        ~MidiDeviceWatcher()
        {
            this.deviceWatcher.Added -= OnDeviceAdded;
            this.deviceWatcher.Removed -= OnDeviceRemoved;
            this.deviceWatcher.Updated -= OnDeviceUpdated;
            this.deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
        }

        /// <summary>
        /// Start the Device Watcher
        /// </summary>
        internal void Start()
        {
            if(this.deviceWatcher.Status != DeviceWatcherStatus.Started)
            {
                this.deviceWatcher.Start();
            }
        }

        /// <summary>
        /// Stop the Device Watcher
        /// </summary>
        internal void Stop()
        {
            if(this.deviceWatcher.Status != DeviceWatcherStatus.Stopped)
            {
                this.deviceWatcher.Stop();
            }
        }

        /// <summary>
        /// Get the DeviceInformationCollection
        /// </summary>
        /// <returns></returns>
        private DeviceInformationCollection GetDeviceInformationCollection()
        {
            return this.deviceInformationCollection;
        }

        /// <summary>
        /// Add any connected MIDI devices to the list
        /// </summary>
        private async void UpdateDevices()
        {
            // Get a list of all MIDI devices
            this.deviceInformationCollection = await DeviceInformation.FindAllAsync(this.midiSelector);
            if ((this.deviceInformationCollection != null) && (this.deviceInformationCollection.Count > 0))
            {
                Debug.WriteLine(GetDeviceInformationCollection());
            }
            RemoveUnknownDevices(deviceInformationCollection);
            AddNewDevices(deviceInformationCollection);
        }

        private void AddNewDevices(DeviceInformationCollection knownDevices)
        {
            List<DeviceInformation> newDevices = new List<DeviceInformation>();

            foreach (var device in knownDevices)
            {
                if (!DeviceCollection.Where(s => s.Id == device.Id).Any())
                {
                    newDevices.Add(device);
                }
            }
            foreach (var device in newDevices)
            {
                foreach (var key in device.Properties.Keys)
                {
                    Debug.WriteLine(key +" "+ device.Properties[key]);
                }
                DeviceCollection.Add(new UwpMidiPortDetails(device));
            }
        }

        private void RemoveUnknownDevices(DeviceInformationCollection knownDevices)
        {
            List<IMidiPortDetails> removables = new List<IMidiPortDetails>();
            
            foreach (var device in DeviceCollection)
            {
                if (knownDevices.Where(s => s.Id == device.Id).Any())
                {
                    removables.Add(device);
                }
            }
            foreach (var device in removables)
            {
                DeviceCollection.Remove(device);
            }
        }

        /// <summary>
        /// Update UI on device added
        /// </summary>
        /// <param name="sender">The active DeviceWatcher instance</param>
        /// <param name="args">Event arguments</param>
        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            // If all devices have been enumerated
            if (this.enumerationCompleted)
            {
                await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    // Update the device list
                    DeviceCollection.Add(new UwpMidiPortDetails(args));                    
                });
            }
        }

        /// <summary>
        /// Update UI on device removed
        /// </summary>
        /// <param name="sender">The active DeviceWatcher instance</param>
        /// <param name="args">Event arguments</param>
        private async void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // If all devices have been enumerated
            if (this.enumerationCompleted)
            {
                await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    // Update the device list
                    IMidiPortDetails query = DeviceCollection.Where(s => s.Id == args.Id).FirstOrDefault();
                    if (query != null)
                    {
                        DeviceCollection.Remove(query);
                    }
                });
            }
        }

        /// <summary>
        /// Update UI on device updated
        /// </summary>
        /// <param name="sender">The active DeviceWatcher instance</param>
        /// <param name="args">Event arguments</param>
        private async void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // If all devices have been enumerated
            if (this.enumerationCompleted)
            {
                await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    // Update the device list
                    UpdateDevices();
                });
            }
        }

        /// <summary>
        /// Update UI on device enumeration completed.
        /// </summary>
        /// <param name="sender">The active DeviceWatcher instance</param>
        /// <param name="args">Event arguments</param>
        private async void OnEnumerationCompleted(DeviceWatcher sender, object args)
        {
            this.enumerationCompleted = true;

            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                // Update the device list
                UpdateDevices();
            });
        }
    }
}
