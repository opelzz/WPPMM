using Kazyx.RemoteApi;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Kazyx.WPPMM.CameraManager
{
    public class EventObserver
    {
        private readonly CameraApiClient client;

        private int failure_count = 0;

        private const int RETRY_LIMIT = 3;

        private const int RETRY_INTERVAL_SEC = 3;

        private CameraStatus status;

        private Action OnStop = null;

        private ApiVersion version = ApiVersion.V1_0;

        private BackgroundWorker worker = new BackgroundWorker()
        {
            WorkerReportsProgress = false,
            WorkerSupportsCancellation = true
        };

        public EventObserver(CameraApiClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status">Status object to update</param>
        /// <param name="OnDetectDifference">Called when the parameter has been changed</param>
        /// <param name="OnStop">Called when event observation is finished with error</param>
        ///
        public async void Start(CameraStatus status, Action OnStop, ApiVersion version)
        {
            Debug.WriteLine("EventObserver.Start");
            if (status == null || OnStop == null)
            {
                throw new ArgumentNullException();
            }
            status.InitEventParams();
            this.status = status;
            this.OnStop = OnStop;
            this.version = version;
            failure_count = RETRY_LIMIT;
            worker.DoWork += AnalyzeEventData;
            try
            {
                var res = await client.GetEventAsync(false, version);
                OnSuccess(res);
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        /// <summary>
        /// Force finish event observation. Any callbacks will not be invoked after this.
        /// </summary>
        public void Stop()
        {
            Debug.WriteLine("EventObserver.Stop");
            Deactivate();
        }

        public async void Refresh()
        {
            Debug.WriteLine("EventObserver.Refresh");
            try
            {
                var res = await client.GetEventAsync(false, version);
                Debug.WriteLine("GetEvent for refresh success");
                if (status != null)
                {
                    UpdateIfRequired(status, res);
                }
            }
            catch (RemoteApiException)
            {
                Debug.WriteLine("GetEvent failed");
            }
        }

        private async void OnError(StatusCode code)
        {
            switch (code)
            {
                case StatusCode.Timeout:
                    Debug.WriteLine("GetEvent timeout without any event. Retry for the next event");
                    Call();
                    return;
                case StatusCode.NotAcceptable:
                case StatusCode.CameraNotReady:
                case StatusCode.IllegalState:
                case StatusCode.ServiceUnavailable:
                case StatusCode.Any:
                    if (failure_count++ < RETRY_LIMIT)
                    {
                        Debug.WriteLine("GetEvent failed - retry " + failure_count + ", status: " + code);
                        await Task.Delay(TimeSpan.FromSeconds(RETRY_INTERVAL_SEC));
                        Call();
                        return;
                    }
                    break;
                case StatusCode.DuplicatePolling:
                    Debug.WriteLine("GetEvent failed duplicate polling");
                    return;
                default:
                    Debug.WriteLine("GetEvent failed with code: " + code);
                    break;
            }

            var TmpOnStop = OnStop;
            if (TmpOnStop != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    TmpOnStop.Invoke();
                });
            }

            Debug.WriteLine("GetEvent Error limit: deactivate now");
            Deactivate();
        }

        private void OnSuccess(Event data)
        {
            failure_count = 0;
            try
            {
                worker.RunWorkerAsync(data);
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }

        private void AnalyzeEventData(object sender, DoWorkEventArgs e)
        {
            if (status == null)
            {
                return;
            }

            UpdateIfRequired(status, e.Argument as Event);

            Call();
        }

        private void UpdateIfRequired(CameraStatus target, Event data)
        {
            StatusUpdater.AvailableApis(target, data.AvailableApis);

            StatusUpdater.CameraStatus(target, data.CameraStatus);

            StatusUpdater.LiveviewAvailability(target, data.LiveviewAvailable);

            StatusUpdater.PostviewSize(target, data.PostviewSizeInfo);

            StatusUpdater.SelfTimer(target, data.SelfTimerInfo);

            StatusUpdater.ShootMode(target, data.ShootModeInfo);

            StatusUpdater.ZoomInfo(target, data.ZoomInfo);

            StatusUpdater.ExposureMode(target, data.ExposureMode);

            StatusUpdater.FNumber(target, data.FNumber);

            StatusUpdater.ShutterSpeed(target, data.ShutterSpeed);

            StatusUpdater.ISO(target, data.ISOSpeedRate);

            StatusUpdater.EvInfo(target, data.EvInfo);

            StatusUpdater.ProgramShift(target, data.ProgramShiftActivated);

            StatusUpdater.FocusStatus(target, data.FocusStatus);

            StatusUpdater.BeepMode(target, data.BeepMode);

            StatusUpdater.SteadyMode(target, data.SteadyMode);

            StatusUpdater.ViewAngle(target, data.ViewAngle);

            StatusUpdater.MovieQuality(target, data.MovieQuality);

            StatusUpdater.Storages(target, data.StorageInfo);

            StatusUpdater.LiveviewOrientation(target, data.LiveviewOrientation);

            StatusUpdater.PictureUrls(target, data.PictureUrls);

            StatusUpdater.StillSize(target, data.StillImageSize, client);

            StatusUpdater.WhiteBalance(target, data.WhiteBalance, client);

            StatusUpdater.FlashMode(target, data.FlashMode);

            StatusUpdater.FocusMode(target, data.FocusMode);

            StatusUpdater.TouchFocusStatus(target, data.TouchAFStatus);            
        }

        private async void Call()
        {
            if (status == null)
            {
                return;
            }
            try
            {
                var res = await client.GetEventAsync(true, version);
                OnSuccess(res);
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        private void Deactivate()
        {
            Debug.WriteLine("EventObserver deactivated");
            status = null;
            OnStop = null;
            worker.DoWork -= AnalyzeEventData;
        }
    }
}
