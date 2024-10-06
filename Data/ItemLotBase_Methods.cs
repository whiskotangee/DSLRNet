using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSLRNet.Data
{
    public partial class ItemLotBase
    {
        public void SetPropertyByName(string name, object value)
        {
            this.GetType().GetProperty(name).SetValue(this, value);
        }

        public T GetValue<T>(string propertyName)
        {
            return (T)this.GetType().GetProperty(propertyName).GetValue(this);
        }
    }
}
