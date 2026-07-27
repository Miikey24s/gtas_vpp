namespace gtas_vpp_fe.Helpers
{
    public static class ObjectExtensions
    {
        public static object? GetPropertyValue(this object obj, string propertyName, Type propertyType)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object? value)
        {
            obj.GetType().GetProperty(propertyName)?.SetValue(obj, value);
        }
    }
}
