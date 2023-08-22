using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plus.Utilities.FigureData;
public interface IFigureDataUtility
{
    FigureData FigureData { get; }
    Task<FigureData> InitFromJsonAsync();
}