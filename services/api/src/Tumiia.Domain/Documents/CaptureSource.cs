using System.Text.Json.Serialization;

namespace Tumiia.Domain.Documents;

/// <summary>How the file reached TUMIIA. Written as <c>drag_drop</c>, <c>file_picker</c> or <c>mobile_upload</c>.</summary>
public enum CaptureSource
{
    [JsonStringEnumMemberName("drag_drop")]
    DragDrop,

    [JsonStringEnumMemberName("file_picker")]
    FilePicker,

    [JsonStringEnumMemberName("mobile_upload")]
    MobileUpload,
}
