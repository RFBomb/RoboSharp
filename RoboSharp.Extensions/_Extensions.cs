
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RoboSharp.Extensions.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e592a7369ca3dd96390ddfbf07df5c714be66670ed4230e4c8ecdb4ccc46072cda33676b502f119841e870aa5afc93cea9c56799a81ddab5fb44a2b9f9e93625c25aa46b51e25671f3dc971f2e44e770fb3f66bb29de8b932a6c24c40656cc43b807c5a7e532f79a86730ea8f930f020bd7757917fa45eb2e54324c60f588cc5")]
namespace RoboSharp.Extensions
{

#if NETSTANDARD2_0 || NETFRAMEWORK
    internal interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
#endif

    internal static class SystemExtensions
    {
        public static async Task CatchCancellation(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }catch(OperationCanceledException)
            {

            }
        }

        public static async Task<T> CatchCancellation<T>(this Task<T> task)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return default;
            }
        }

        public static ConfiguredTaskAwaitable CatchCancellation(this Task task, bool continueOnCapturedContext) => CatchCancellation(task).ConfigureAwait(continueOnCapturedContext);
        public static ConfiguredTaskAwaitable<T> CatchCancellation<T>(this Task<T> task, bool continueOnCapturedContext) => CatchCancellation(task).ConfigureAwait(continueOnCapturedContext);
    }
}
