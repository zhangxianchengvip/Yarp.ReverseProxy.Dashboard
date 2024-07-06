using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarp.ReverseProxy.Dashboard
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            PathBase = string.Empty;
            PathMatch = "/yarp";
            AllowAnonymousExplicit = true;
            StatsPollingInterval = 2000;
        }

        public string PathBase { get; set; }

        public string PathMatch { get; set; }

        public bool AllowAnonymousExplicit { get; set; }

        public string? AuthorizationPolicy { get; set; }

        public int StatsPollingInterval { get; set; }
    }
}
