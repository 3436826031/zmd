using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Zmd;

internal sealed class FileDropTarget : IDropTarget
{
    private const short CfHdrop = 15;
    private const int DvEFormatEtc = unchecked((int)0x80040064);
    private const int DragDropSCancel = 0x00040101;
    private const int DropEffectNone = 0;
    private const int DropEffectCopy = 1;

    private readonly Action<IReadOnlyList<string>> filesDropped;

    public FileDropTarget(Action<IReadOnlyList<string>> filesDropped)
    {
        this.filesDropped = filesDropped;
    }

    public int DragEnter(ComDataObject dataObject, int keyState, PointL point, ref int effect)
    {
        effect = HasFiles(dataObject) ? DropEffectCopy : DropEffectNone;
        return 0;
    }

    public int DragOver(int keyState, PointL point, ref int effect)
    {
        effect = DropEffectCopy;
        return 0;
    }

    public int DragLeave()
    {
        return 0;
    }

    public int Drop(ComDataObject dataObject, int keyState, PointL point, ref int effect)
    {
        var paths = FilePaths(dataObject);
        effect = paths.Count > 0 ? DropEffectCopy : DropEffectNone;
        if (paths.Count > 0)
        {
            filesDropped(paths);
        }

        return 0;
    }

    private static bool HasFiles(ComDataObject dataObject)
    {
        var format = FileDropFormat();
        return dataObject.QueryGetData(ref format) == 0;
    }

    private static IReadOnlyList<string> FilePaths(ComDataObject dataObject)
    {
        var format = FileDropFormat();
        dataObject.GetData(ref format, out var medium);
        if (medium.tymed != TYMED.TYMED_HGLOBAL || medium.unionmember == nint.Zero)
        {
            NativeMethods.ReleaseStgMedium(ref medium);
            return Array.Empty<string>();
        }

        try
        {
            return TerminalControl.DroppedFilePaths(medium.unionmember);
        }
        finally
        {
            NativeMethods.ReleaseStgMedium(ref medium);
        }
    }

    private static FORMATETC FileDropFormat()
    {
        return new FORMATETC
        {
            cfFormat = CfHdrop,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            ptd = nint.Zero,
            tymed = TYMED.TYMED_HGLOBAL
        };
    }
}
