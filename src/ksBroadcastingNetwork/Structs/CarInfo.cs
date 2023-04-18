﻿using System.Collections.Generic;
using KLPlugins.DataExport.Car;

namespace KLPlugins.DataExport.ksBroadcastingNetwork.Structs
{
    class CarInfo
    {
        public ushort CarIndex { get; }
        public CarType CarModelType { get; internal set; }
        public CarClass CarClass { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public TeamCupCategory CupCategory { get; internal set; }
        public int CurrentDriverIndex { get; internal set; }
        public IList<DriverInfo> Drivers { get; } = new List<DriverInfo>();
        public NationalityEnum Nationality { get; internal set; }

        public CarInfo(ushort carIndex)
        {
            CarIndex = carIndex;
        }

        internal void AddDriver(DriverInfo driverInfo)
        {
            Drivers.Add(driverInfo);
        }

        public string GetCurrentDriverName()
        {
            if (CurrentDriverIndex < Drivers.Count)
                return Drivers[CurrentDriverIndex].LastName;
            return "nobody(?)";
        }
    }
}
