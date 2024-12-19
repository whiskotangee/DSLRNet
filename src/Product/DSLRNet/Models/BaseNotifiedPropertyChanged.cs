namespace DSLRNet.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

public class BaseModel<T> 
    : INotifyPropertyChanged, IDataErrorInfo where T : class
{
    public T OriginalObject { get; protected set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string Error => null;

    public string this[string columnName]
    {
        get
        {
            string result = null;
            var property = GetType().GetProperty(columnName);
            if (property != null)
            {
                var value = property.GetValue(this);
                var validationAttributes = property.GetCustomAttributes(typeof(ValidationAttribute), true);
                foreach (ValidationAttribute attribute in validationAttributes)
                {
                    if (!attribute.IsValid(value))
                    {
                        result = attribute.ErrorMessage;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
