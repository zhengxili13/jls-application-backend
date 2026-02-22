using System.Collections.Generic;

namespace JLSDataModel.AdminViewModel;

public class ListViewModelWithCount<T>
{
    public List<T> Content { get; set; }

    public int Count { get; set; }
}