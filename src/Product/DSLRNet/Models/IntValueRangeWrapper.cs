namespace DSLRNet.Models;

using System.Collections.Generic;
using DSLRNet.Core.Common;

public class IntValueRangeWrapper : BaseModel<IntValueRange>
{
    private readonly IntValueRange _range;

    public IntValueRangeWrapper(IntValueRange range)
    {
        _range = range;
        OriginalObject = _range;
    }

    public int Min
    {
        get => _range.Min;
        set
        {
            if (_range.Min != value)
            {
                _range.Min = value;
                OnPropertyChanged();
            }
        }
    }

    public int Max
    {
        get => _range.Max;
        set
        {
            if (_range.Max != value)
            {
                _range.Max = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Contains(int value) => _range.Contains(value);

    public static IntValueRangeWrapper CreateFrom(IEnumerable<int> values) => new(IntValueRange.CreateFrom(values));

    public IEnumerable<int> ToRangeOfValues() => _range.ToRangeOfValues();
}

