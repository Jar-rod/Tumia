using System.Text.Json.Serialization;

namespace Tumiia.Domain.Documents;

/// <summary>How the file reached TUMIIA. Written as <c>drag_drop</c>, <c>file_picker</c>, <c>mobile_upload</c> or <c>paste</c>.</summary>
public enum CaptureSource
{
    [JsonStringEnumMemberName("drag_drop")]
    DragDrop,

    [JsonStringEnumMemberName("file_picker")]
    FilePicker,

    [JsonStringEnumMemberName("mobile_upload")]
    MobileUpload,

    /// <summary>Text or a message the user pasted in; it is stored as a .txt original.</summary>
    [JsonStringEnumMemberName("paste")]
    Paste,
}
