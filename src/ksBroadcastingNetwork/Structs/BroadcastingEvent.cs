﻿
namespace KLPlugins.DataExport.ksBroadcastingNetwork.Structs
{
    class BroadcastingEvent
    {
        public BroadcastingCarEventType Type { get; internal set; }
        public string Msg { get; internal set; }
        public int TimeMs { get; internal set; }
        public int CarId { get; internal set; }
        public CarInfo CarData { get; internal set; }
    }
}
