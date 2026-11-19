namespace UniGameUI
{
    public readonly struct UIPopupResult<T>
    {
        internal UIPopupResult(UINavResult status, bool hasValue = false, T value = default)
        {
            Status = status;
            HasValue = hasValue;
            Value = value;
        }

        public UINavResult Status { get; }
        public bool HasValue { get; }
        public T Value { get; }

        public T GetValueOrDefault(T fallback = default) => HasValue ? Value : fallback;
    }
}
