using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DART.BlackduckAnalysis
{
    public interface IBlackduckReportGenerator
    {
        Task GenerateReport();

        Task Cleanup();
    }
}
