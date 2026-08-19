using System.Runtime.InteropServices;

namespace WEB.Services.Versioning
{
    public class AppVersionInfo
    {
        public string Version => BuildInfo.Version;

        public string BuildNumber => BuildInfo.BuildNumber;

        public string BuildDate => BuildInfo.BuildDate;

        public string Framework => RuntimeInformation.FrameworkDescription;
    }
}
