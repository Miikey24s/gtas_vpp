namespace gtas_vpp_fe.Models;

public class GlobalStorageModel
{
    public string Theme { get; set; }
    public bool isFirstRender { get; set; } = true;
    public List<GlobalStorageHeaderModel> Header { get; set; }

    public GlobalStorageModel()
    {
        Theme = "";
        Header = [];
    }

    private bool _isDisposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
        }
    }
}

public class GlobalStorageHeaderModel
{
    public required string PageName { get; set; }
    public required List<GlobalStorageFieldModel> Fields { get; set; }
}

public class GlobalStorageFieldModel
{
    public required string FieldName { get; set; }
    public required string FieldValue { get; set; }
}
