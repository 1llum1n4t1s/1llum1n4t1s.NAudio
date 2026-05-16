using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio DeviceTopology
    /// </summary>
    public class DeviceTopology : IDisposable
    {
        private readonly IDeviceTopology deviceTopologyInterface;
        private bool disposed;

        internal DeviceTopology(IDeviceTopology deviceTopology)
        {
            deviceTopologyInterface = deviceTopology;
        }

        /// <summary>
        /// Retrieves the number of connections associated with this device-topology object
        /// </summary>
        public uint ConnectorCount
        {
            get
            {
                deviceTopologyInterface.GetConnectorCount(out var count);
                return count;
            }
        }

        /// <summary>
        /// Retrieves the connector at the supplied index
        /// </summary>
        public Connector GetConnector(uint index)
        {
            deviceTopologyInterface.GetConnector(index, out var connectorInterface);
            return new Connector(connectorInterface);
        }

        /// <summary>
        /// Retrieves the device id of the device represented by this device-topology object
        /// </summary>
        public string DeviceId
        {
            get
            {
                deviceTopologyInterface.GetDeviceId(out var result);
                return result;
            }
        }

        /// <summary>
        /// IDeviceTopology COM オブジェクトを解放する。
        /// MMDevice.Dispose 経由でも呼ばれる。
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (deviceTopologyInterface != null)
            {
                Marshal.ReleaseComObject(deviceTopologyInterface);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ファイナライザ。GC スレッドからの COM 解放を避けるため Dispose 漏れは警告のみ。
        /// </summary>
        ~DeviceTopology()
        {
            if (!disposed)
            {
                System.Diagnostics.Debug.WriteLine(
                    "WARNING: DeviceTopology が Dispose されずに finalize された。COM オブジェクトがリーク可能性あり。");
            }
        }
    }
}
