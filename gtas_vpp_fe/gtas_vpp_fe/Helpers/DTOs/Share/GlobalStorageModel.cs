namespace gtas_vpp_fe.Helpers.DTOs.Share
{
    public class GlobalStorageModel
    {
        public string Theme { get; set; }
        public bool isFirstRender { get; set; } = true;
        public List<GlobalStorageHeaderModel> Header { get; set; }
        public GlobalStorageModel()
        {
            Theme = "";
            Header = new List<GlobalStorageHeaderModel>();
        }
        private bool _isDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    //Do your unmanaged disposing here
                    _isDisposed = true;
                }
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
}
