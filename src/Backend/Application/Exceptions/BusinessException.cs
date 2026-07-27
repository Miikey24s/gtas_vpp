namespace gtas_vpp_be.Service.Exceptions
{
    /// <summary>
    /// Được ném khi request vi phạm business invariant, ví dụ sai kỳ, quá hạn hoặc
    /// chuyển trạng thái không hợp lệ. <c>ExceptionHandlingMiddleware</c> ánh xạ
    /// exception này thành HTTP 422 (hoặc 400).
    /// </summary>
    public class BusinessException : System.Exception
    {
        public BusinessException(string message) : base(message)
        {
        }

        public BusinessException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
