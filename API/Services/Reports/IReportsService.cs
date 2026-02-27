

using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace API.Services.Reports
{
    public interface IReportsService
    {
        public Task GetReportScaProducts(string Order);
    }
}
