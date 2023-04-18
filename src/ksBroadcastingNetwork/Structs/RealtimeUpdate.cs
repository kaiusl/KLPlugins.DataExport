﻿
using System;

namespace KLPlugins.DataExport.ksBroadcastingNetwork.Structs
{
    class RealtimeUpdate
    {
        public int EventIndex { get; internal set; }
        public int SessionIndex { get; internal set; }
        public SessionPhase Phase { get; internal set; }
        public TimeSpan SessionRunningTime { get; internal set; } // Time the session has been running
        public TimeSpan RemainingTime { get; internal set; } // Seems to be zero always
        public TimeSpan SystemTime { get; internal set; } // Real world time
        public float RainLevel { get; internal set; }
        public float Clouds { get; internal set; }
        public float Wetness { get; internal set; }
        public LapInfo BestSessionLap { get; internal set; }
        public ushort BestLapCarIndex { get; internal set; }
        public ushort BestLapDriverIndex { get; internal set; }
        public int FocusedCarIndex { get; internal set; }
        public string ActiveCameraSet { get; internal set; }
        public string ActiveCamera { get; internal set; }
        public bool IsReplayPlaying { get; internal set; }
        public float ReplaySessionTime { get; internal set; }
        public float ReplayRemainingTime { get; internal set; }
        public TimeSpan SessionEndTime { get; internal set; } // Seems to be zero always
        public TimeSpan SessionRemainingTime { get; internal set; } // Time left until the session end
        public RaceSessionType SessionType { get; internal set; }
        public byte AmbientTemp { get; internal set; }
        public byte TrackTemp { get; internal set; }
        public string CurrentHudPage { get; internal set; }
        public DateTime RecieveTime { get; internal set; }

    }
}
