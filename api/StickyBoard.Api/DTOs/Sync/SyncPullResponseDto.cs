public sealed class SyncPullResponseDto
{
    public DateTime ServerTime { get; init; }

    public List<object> Boards { get; set; } = [];
    public List<object> Sections { get; set; } = [];
    public List<object> Tabs { get; set; } = [];
    public List<object> Cards { get; set; } = [];
    public List<object> Files { get; set; } = [];
    public List<object> Activities { get; set; } = [];
    public List<object> Deleted { get; set; } = [];
}