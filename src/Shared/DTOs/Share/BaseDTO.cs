namespace gtas_vpp_shared.DTOs.Share
{
    public class BaseDTO
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsDeleted { get; set; }
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
                    // Giải phóng tài nguyên unmanaged tại đây.
                    _isDisposed = true;
                }
            }
        }
    }
}

