// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AudioSessionManager
    /// 
    /// Designed to manage audio sessions and in particuar the
    /// SimpleAudioVolume interface to adjust a session volume
    /// </summary>
    public class AudioSessionManager
    {
        private readonly IAudioSessionManager audioSessionInterface;
        private readonly IAudioSessionManager2 audioSessionInterface2;
        private AudioSessionNotification audioSessionNotification;
        private SessionCollection sessions;

        private SimpleAudioVolume simpleAudioVolume;
        private AudioSessionControl audioSessionControl;
        private bool disposed;

        /// <summary>
        /// Session created delegate
        /// </summary>
        public delegate void SessionCreatedDelegate(object sender, IAudioSessionControl newSession);
        
        /// <summary>
        /// Occurs when audio session has been added (for example run another program that use audio playback).
        /// </summary>
        public event SessionCreatedDelegate OnSessionCreated;

        internal AudioSessionManager(IAudioSessionManager audioSessionManager)
        {
            audioSessionInterface = audioSessionManager;
            audioSessionInterface2 = audioSessionManager as IAudioSessionManager2;

            RefreshSessions();
        }

        /// <summary>
        /// SimpleAudioVolume object
        /// for adjusting the volume for the user session
        /// </summary>
        public SimpleAudioVolume SimpleAudioVolume
        {
            get
            {
                if (simpleAudioVolume == null)
                {
                    audioSessionInterface.GetSimpleAudioVolume(Guid.Empty, 0, out var simpleAudioInterface);

                    simpleAudioVolume = new SimpleAudioVolume(simpleAudioInterface);
                }
                return simpleAudioVolume;
            }
        }

        /// <summary>
        /// AudioSessionControl object
        /// for registring for callbacks and other session information
        /// </summary>
        public AudioSessionControl AudioSessionControl
        {
            get
            {
                if (audioSessionControl == null)
                {
                    audioSessionInterface.GetAudioSessionControl(Guid.Empty, 0, out var audioSessionControlInterface);

                    audioSessionControl = new AudioSessionControl(audioSessionControlInterface);
                }
                return audioSessionControl;
            }
        }

        internal void FireSessionCreated(IAudioSessionControl newSession)
        {
            OnSessionCreated?.Invoke(this, newSession);
        }

        /// <summary>
        /// Refresh session of current device.
        /// </summary>
        public void RefreshSessions()
        {
            UnregisterNotifications();

            if (audioSessionInterface2 != null)
            {
                Marshal.ThrowExceptionForHR(audioSessionInterface2.GetSessionEnumerator(out var sessionEnum));
                sessions = new SessionCollection(sessionEnum);

                audioSessionNotification = new AudioSessionNotification(this);
                Marshal.ThrowExceptionForHR(audioSessionInterface2.RegisterSessionNotification(audioSessionNotification));
            }
        }

        /// <summary>
        /// Returns list of sessions of current device.
        /// </summary>
        public SessionCollection Sessions => sessions;

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            // COM 操作は明示 Dispose 経由 (disposing=true) のみ実行する。
            // ファイナライザ (disposing=false) から STA バインド COM を
            // GC スレッド (MTA) で UnregisterSessionNotification / ReleaseComObject すると
            // RPC_E_WRONG_THREAD / AccessViolation になるため触らない (COM Lifecycle 統一方針)。
            if (disposing)
            {
                UnregisterNotifications();
                Marshal.ReleaseComObject(audioSessionInterface);
            }
        }

        private void UnregisterNotifications()
        {
            sessions = null;

            if (audioSessionNotification != null && audioSessionInterface2 != null)
            {
                audioSessionInterface2.UnregisterSessionNotification(audioSessionNotification);
                audioSessionNotification = null;
            }
        }

        /// <summary>
        /// Finalizer. COM オブジェクトには触れず、Dispose 漏れの警告のみ出す。
        /// GC スレッドからの STA-COM 操作を避けるため、最終解放は CLR の RCW に委ねる
        /// (MediaType / PropertyStore / MMDevice 等と同じ統一方針)。
        /// </summary>
        ~AudioSessionManager()
        {
            System.Diagnostics.Debug.WriteLine("AudioSessionManager: Dispose() が呼ばれていません。ファイナライザでは COM を解放しません (cross-thread COM 回避)。");
        }
    }
}
