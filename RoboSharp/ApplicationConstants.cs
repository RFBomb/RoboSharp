using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("RoboSharp.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e592a7369ca3dd96390ddfbf07df5c714be66670ed4230e4c8ecdb4ccc46072cda33676b502f119841e870aa5afc93cea9c56799a81ddab5fb44a2b9f9e93625c25aa46b51e25671f3dc971f2e44e770fb3f66bb29de8b932a6c24c40656cc43b807c5a7e532f79a86730ea8f930f020bd7757917fa45eb2e54324c60f588cc5")]
[assembly: InternalsVisibleTo("RoboSharp.Benchmarks, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e592a7369ca3dd96390ddfbf07df5c714be66670ed4230e4c8ecdb4ccc46072cda33676b502f119841e870aa5afc93cea9c56799a81ddab5fb44a2b9f9e93625c25aa46b51e25671f3dc971f2e44e770fb3f66bb29de8b932a6c24c40656cc43b807c5a7e532f79a86730ea8f930f020bd7757917fa45eb2e54324c60f588cc5")]
namespace RoboSharp
{
    internal static class ApplicationConstants
    {

        /// <summary>
        /// The static constructor for the class to take care of any setup / fixes required before running any operations.
        /// </summary>
        static ApplicationConstants()
        {
#if !NETFRAMEWORK // Ensure that encoding 437 is supported, which is only available in NetFramework by default
            CodePagesEncodingProvider.Instance.GetEncoding(437);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        /// <summary> Request this null object used to ensure the static constructor executes </summary>
        internal static object Initializer => null;

        internal static Dictionary<string, string> ErrorCodes = new Dictionary<string, string>()
        {
            { "ERROR 33 (0x00000021)", "The process cannot access the file because another process has locked a portion of the file." },
            { "ERROR 32 (0x00000020)", "The process cannot access the file because it is being used by another process." },
            { "ERROR 5 (0x00000005)", "Access is denied." }
        };
    }
}
