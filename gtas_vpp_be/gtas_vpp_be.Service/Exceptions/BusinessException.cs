namespace gtas_vpp_be.Service.Exceptions
{
    /// <summary>
    /// Thrown when a request violates a business invariant (e.g. wrong period,
    /// deadline passed, invalid status transition). Mapped to HTTP 422 (or 400)
    /// by <c>ExceptionHandlingMiddleware</c>.
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
