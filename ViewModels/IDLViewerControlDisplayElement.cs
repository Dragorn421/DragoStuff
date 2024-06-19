using F3DZEX.Command;
using OpenTK.Mathematics;

namespace Z64Utils_Avalonia;

public interface IDLViewerControlDisplayElement { }
public class DLViewerControlDListDisplayElement : IDLViewerControlDisplayElement
{
    public Dlist dList;
}
public class DLViewerControlDlistWithMatrixDisplayElement : IDLViewerControlDisplayElement
{
    public Dlist dList;
    public Matrix4 mtx;
}
