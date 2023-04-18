//using ACSharedMemory.ACC.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameReaderCommon;
using KLPlugins.DataExport.Car;
using KLPlugins.DataExport.ksBroadcastingNetwork;
using KLPlugins.DataExport.ksBroadcastingNetwork.Structs;
using KLPlugins.DataExport.Realtime;
using KLPlugins.DataExport.Track;
using SimHub.Plugins;

namespace KLPlugins.DataExport
{
    class CarSplinePos
    {
        // Index into Cars array
        public int CarIdx = -1;
        // Corresponding splinePosition
        public double SplinePos = 0;

        public CarSplinePos(int idx, double pos)
        {
            CarIdx = idx;
            SplinePos = pos;
        }
    }

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    class Values : IDisposable
    {
        public RealtimeData RealtimeData { get; private set; }
        public static TrackData TrackData { get; private set; }

        // Idea with cars is to store one copy of data
        // We keep cars array sorted in overall position order
        // Other orderings are stored in different array containing indices into Cars list
        internal List<CarData> Cars { get; private set; }
        internal ACCUdpRemoteClient BroadcastClient { get; private set; }

        //internal ACCRawData RawData { get; private set; }

        private List<ushort> _lastUpdateCarIds = new List<ushort>();
        private ACCUdpRemoteClientConfig _broadcastConfig;

        internal Values()
        {
            Cars = new List<CarData>();
            _broadcastConfig = new ACCUdpRemoteClientConfig("127.0.0.1", "DataExportPlugin", 10);
        }

        public CarData GetCar(int i) => Cars.ElementAtOrDefault(i);

        internal void Reset()
        {
            if (BroadcastClient != null)
            {
                DisposeBroadcastClient();
            }
            RealtimeData = null;
            TrackData = null;
            Cars.Clear();
            _outdata?.Clear();
            _bestLaps?.Clear();
        }

        #region IDisposable Support
        ~Values()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    DisposeBroadcastClient();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        internal void OnDataUpdate(PluginManager pm, GameData data)
        {
            //RawData = (ACCRawData)data.NewData.GetRawDataObject();
        }

        internal void OnGameStateChanged(bool running, PluginManager manager)
        {
            if (running)
            {
                if (BroadcastClient != null)
                {
                    DisposeBroadcastClient();
                }
                ConnectToBroadcastClient();
            }
            else
            {
                Reset();
            }
        }

        internal CarData GetCar(int i, int?[] idxs)
        {
            if (i > idxs.Length - 1) return null;
            var idx = idxs[i];
            if (idx == null) return null;
            return Cars[(int)idx];
        }

        #region Broadcast client connection

        internal void ConnectToBroadcastClient()
        {
            BroadcastClient = new ACCUdpRemoteClient(_broadcastConfig);
            BroadcastClient.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            BroadcastClient.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            BroadcastClient.MessageHandler.OnRealtimeUpdate += OnBroadcastRealtimeUpdate;
            BroadcastClient.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
        }

        internal async void DisposeBroadcastClient()
        {
            if (BroadcastClient != null)
            {
                await BroadcastClient.ShutdownAsync();
                BroadcastClient.Dispose();
                BroadcastClient = null;
            }
        }

        // Updates come as:
        // New entry list
        // All the current CarInfos
        // Track data
        // *** Repeating
        //    Realtime update
        //    Realtime update for all the cars
        // ***
        // New entry list if found new car or driver

        private void OnBroadcastRealtimeUpdate(string sender, RealtimeUpdate update)
        {
            if (RealtimeData == null)
            {
                RealtimeData = new RealtimeData(update);
                return;
            }
            else
            {
                RealtimeData.OnRealtimeUpdate(update);
            }

            if (RealtimeData.IsNewSession)
            {
                // Clear all data at the beginning of session
                // Technically we only need clear parts of the data, but this is simpler
                Cars.Clear();
                BroadcastClient.MessageHandler.RequestEntryList();
                _outdata?.Clear();
                _bestLaps?.Clear();
            }

            if (Cars.Count == 0) return;
            ClearMissingCars();
            #region Local functions

            void ClearMissingCars()
            {
                // Idea here is that realtime updates come as repeating loop of
                // * Realtime update
                // * RealtimeCarUpdate for each car
                // Thus if we keep track of cars seen in the last loop, we can remove cars that have left the session
                // However as we receive data as UDP packets, there is a possibility that some packets go missing
                // Then we could possibly remove cars that are actually still in session
                // Thus we keep track of how many times in order each car hasn't received the update
                // If it's larger than some number, we remove the car
                if (_lastUpdateCarIds.Count != 0)
                {
                    foreach (var car in Cars)
                    {
                        if (!_lastUpdateCarIds.Contains(car.CarIndex))
                        {
                            car.MissedRealtimeUpdates++;
                        }
                        else
                        {
                            car.MissedRealtimeUpdates = 0;
                        }
                    }
                }
                _lastUpdateCarIds.Clear();
            }

            #endregion

        }

        private void OnEntryListUpdate(string sender, CarInfo carInfo)
        {
            // Add new cars if not already added, update car info of all the cars (adds new drivers if some were missing)
            var car = Cars.Find(x => x.CarIndex == carInfo.CarIndex);
            if (car == null)
            {
                Cars.Add(new CarData(carInfo, null));
            }
            else
            {
                car.OnEntryListUpdate(carInfo);
            }
        }

        private Dictionary<int, string> _outdata = new Dictionary<int, string>();
        private Dictionary<CarClass, double> _bestLaps = new Dictionary<CarClass, double>();
        private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate update)
        {
            // Update Realtime data of existing cars
            // If found new car, BroadcastClient itself requests new entry list
            if (RealtimeData == null) return;
            var car = Cars.Find(x => x.CarIndex == update.CarIndex);
            if (car == null) return;
            car.OnRealtimeCarUpdate(update, RealtimeData);
            _lastUpdateCarIds.Add(car.CarIndex);

            CreateLapInterpolatorsData(update, car);
        }

        private string _datapath = @"PluginsData\KLPlugins\DataExport\laps_data\raw";
        void CreateLapInterpolatorsData(RealtimeCarUpdate update, CarData car)
        {
            if (!Directory.Exists(_datapath))
            {
                Directory.CreateDirectory(_datapath);
            }

            if (_outdata.ContainsKey(car.CarIndex) && car.NewData.CarLocation != CarLocationEnum.Track)
            {
                _outdata.Remove(car.CarIndex);
            }
            string trackName;
            if (TrackData.TrackId == TrackType.Unknown)
            {
                trackName = TrackData.TrackName;
            }
            else {
                trackName = $"{TrackData.TrackId}";
            }
            var fname = $@"{_datapath}\{trackName}_{car.CarClass}.txt";
            if (!_bestLaps.ContainsKey(car.CarClass))
            {
                if (File.Exists(fname))
                {
                    try
                    {
                        var t = 0.0;

                        foreach (var l in File.ReadLines(fname))
                        {
                            if (l == "") continue;
                            // Data order: splinePositions, lap time in ms, speed in kmh
                            var splits = l.Split(';');
                            double p = float.Parse(splits[0]);
                            t = double.Parse(splits[1]);
                        }
                        _bestLaps[car.CarClass] = t;
                        //DataExport.LogInfo($"New best lap for {car.CarClass}: {TimeSpan.FromSeconds((double)car.NewData.LastLap.Laptime).ToString("mm\\:ss\\.fff")}");


                    }
                    catch (Exception ex)
                    {
                        DataExport.LogError(ex.ToString());
                    }
                }
            }

            if (car.OldData != null && car.NewData.Laps != car.OldData.Laps && car.NewData.IsOnTrack)
            {
                if (!_outdata.ContainsKey(car.CarIndex))
                {
                    _outdata[car.CarIndex] = "";
                    return;
                }

                var thisclass = car.CarClass;

                if (car.NewData?.LastLap?.Laptime != null
                    && car.NewData.LastLap.IsValidForBest
                    && (!_bestLaps.ContainsKey(thisclass) || (car.NewData.LastLap.Laptime < _bestLaps[thisclass]))
                )
                {
                    DataExport.LogInfo($"New best lap for {thisclass}: {TimeSpan.FromSeconds((double)car.NewData.LastLap.Laptime).ToString("mm\\:ss\\.fff")}. Written to {fname}.");
                    _bestLaps[thisclass] = (double)car.NewData.LastLap.Laptime;
                    File.WriteAllText(fname, _outdata[car.CarIndex]);
                }

                _outdata[car.CarIndex] = "";
            }

            if (_outdata.ContainsKey(car.CarIndex))
            {
                if (car.OldData.CurrentLap.Laptime < car.NewData.CurrentLap.Laptime && car.OldData.SplinePosition != car.NewData.SplinePosition)
                {
                    if (_outdata[car.CarIndex] != "")
                    {
                        _outdata[car.CarIndex] += "\n";
                    }
                    _outdata[car.CarIndex] += $"{update.SplinePosition};{update.CurrentLap.Laptime};{update.Kmh};{update.WorldPosX};{update.WorldPosY};{update.Laps}";
                }
            }
        }

        private void OnTrackDataUpdate(string sender, TrackData update)
        {
            _outdata?.Clear();
            _bestLaps?.Clear();

            TrackData = update;
        }

        #endregion

    }
}