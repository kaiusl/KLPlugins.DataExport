namespace KLPlugins.DataExport.Track
{
    class TrackData
    {
        public string TrackName { get; internal set; }
        public TrackType TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        public double SplinePosOffset => TrackId.SplinePosOffset();
    }
}
