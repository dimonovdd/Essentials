using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Contacts
    {
        static Task<Contact> PlatformPickContactAsync() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<ContactsStore> PlatformGetContactsStore(CancellationToken cancellationToken)
            => throw ExceptionUtils.NotSupportedOrImplementedException;

#if !NETSTANDARD1_0
        static IAsyncEnumerable<Contact> PlatformGetAllAsync(CancellationToken cancellationToken) => throw ExceptionUtils.NotSupportedOrImplementedException;
#endif
    }

    public partial class ContactsStore
    {
        public IEnumerable<Contact> GetAllPlatform(CancellationToken cancellationToken = default)
            => throw ExceptionUtils.NotSupportedOrImplementedException;
    }
}
