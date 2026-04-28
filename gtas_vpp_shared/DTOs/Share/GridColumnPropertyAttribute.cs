namespace gtas_vpp_shared.DTOs.Share
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class GridColumnPropertyAttribute : Attribute
    {
        static int count = 0;
        public GridColumnPropertyAttribute(string? displayname = null, string? width = null, bool ignore = false, int index = 0, bool iscompobox = false)
        {
            DisplayName = displayname;
            Width = width;
            Ignore = ignore;

            count = index > 0 ? index : count + 1;
            Index = count;
            IsDropdownList = iscompobox;
        }

        public string? DisplayName { get; private set; }
        public string? Width { get; private set; }
        public bool Ignore { get; private set; }
        public int Index { get; private set; }
        public bool IsDropdownList { get; private set; }
    }
}
