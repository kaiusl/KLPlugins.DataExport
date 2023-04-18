using System.Collections.Generic;
using System.Linq;
using KLPlugins.DataExport.Driver;
using KLPlugins.DataExport.ksBroadcastingNetwork;
using KLPlugins.DataExport.ksBroadcastingNetwork.Structs;
using KLPlugins.DataExport.Realtime;

namespace KLPlugins.DataExport.Car
{
    class CarData
    {
        internal enum OffsetLapUpdateType
        {
            None = 0,
            LapBeforeSpline = 1,
            SplineBeforeLap = 2
        }

        // Information from CarInfo
        public ushort CarIndex { get; }
        public CarType CarModelType { get; internal set; }
        public CarClass CarClass { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public TeamCupCategory TeamCupCategory { get; internal set; }
        private int _currentDriverIndex { get; set; }
        public List<DriverData> Drivers { get; internal set; } = new List<DriverData>();
        public NationalityEnum TeamNationality { get; internal set; }

        // RealtimeCarUpdates
        public RealtimeCarUpdate NewData { get; private set; } = null;
        public RealtimeCarUpdate OldData { get; private set; } = null;

        public int CurrentDriverIndex;
        public DriverData CurrentDriver => Drivers[CurrentDriverIndex];

        internal int MissedRealtimeUpdates { get; set; } = 0;


        ////////////////////////

        internal CarData(CarInfo info, RealtimeCarUpdate update)
        {
            CarIndex = info.CarIndex;
            CarModelType = info.CarModelType;
            CarClass = info.CarClass;
            TeamName = info.TeamName;
            RaceNumber = info.RaceNumber;
            TeamCupCategory = info.CupCategory;
            _currentDriverIndex = info.CurrentDriverIndex;
            CurrentDriverIndex = _currentDriverIndex;
            foreach (var d in info.Drivers)
            {
                AddDriver(d);
            }
            TeamNationality = info.Nationality;

            NewData = update;
        }

        /// <summary>
        /// Return current driver always as first driver. Other drivers in order as they are in drivers list.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public DriverData GetDriver(int i)
        {
            if (i == 0) { return Drivers.ElementAtOrDefault(CurrentDriverIndex); }
            if (i <= CurrentDriverIndex) { return Drivers.ElementAtOrDefault(i - 1); }
            return Drivers.ElementAtOrDefault(i);
        }


        /// <summary>
        /// Updates this cars static info. Should be called when new entry list update for this car is received.
        /// </summary>
        /// <param name="info"></param>
        internal void OnEntryListUpdate(CarInfo info)
        {
            // Only thing that can change is drivers
            // We need to make sure that the order is as specified by new info
            // But also add new drivers. We keep old drivers but move them to the end of list
            // as they might rejoin and then we need to have the old data. (I'm not sure if ACC keeps those drivers or not, but we make sure to keep the data.)
            CurrentDriverIndex = info.CurrentDriverIndex;
            if (Drivers.Count == info.Drivers.Count
                && Drivers.Zip(info.Drivers, (a, b) => a.Equals(b)).All(x => x)
            ) return; // All drivers are same

            // Fix drivers list
            for (int i = 0; i < info.Drivers.Count; i++)
            {
                var currentDriver = Drivers[i];
                var newDriver = info.Drivers[i];
                if (currentDriver.Equals(newDriver)) continue;

                var oldIdx = Drivers.FindIndex(x => x.Equals(newDriver));
                if (oldIdx == -1)
                {
                    // Must be new driver
                    Drivers.Insert(i, new DriverData(newDriver));
                }
                else
                {
                    // Driver is present but it's order has changed
                    var old = Drivers[oldIdx];
                    Drivers.RemoveAt(oldIdx);
                    Drivers.Insert(i, old);
                }
            }
        }


        internal void OnRealtimeCarUpdate(RealtimeCarUpdate update, RealtimeData realtimeData)
        {
            // If the race is finished we don't care about any of the realtime updates anymore.
            // We have set finished positions in ´OnRealtimeUpdate´ and that's really all that matters
            //if (IsFinished) return;

            OldData = NewData;
            NewData = update;
        }

        private void AddDriver(DriverInfo driverInfo)
        {
            Drivers.Add(new DriverData(driverInfo));
        }

    }
}
