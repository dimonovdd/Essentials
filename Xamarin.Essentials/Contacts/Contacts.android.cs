﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;
using Net = Android.Net;

namespace Xamarin.Essentials
{
    public static partial class Contacts
    {
        static async Task<Contact> PlatformPickContactAsync()
        {
            using var intent = new Intent(Intent.ActionPick);
            intent.SetType(ContactsContract.CommonDataKinds.Phone.ContentType);
            var result = await IntermediateActivity.StartAsync(intent, Platform.requestCodePickContact).ConfigureAwait(false);

            if (result?.Data != null)
                return GetContact(result.Data);

            return null;
        }

        static Task<ContactsStore> PlatformGetContactsStore(CancellationToken cancellationToken)
            => null;

        static async IAsyncEnumerable<Contact> PlatformGetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            using var context = Platform.AppContext.ContentResolver;
            using var cursor = context.Query(ContactsContract.Contacts.ContentUri, null, null, null, null);

            if (cursor == null)
                yield break;

            if (cursor?.MoveToFirst() ?? false)
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var contact = GetContact(cursor, context, ContactsContract.Contacts.InterfaceConsts.Id);
                    if (contact != null)
                        yield return contact;
                }
                while (cursor.MoveToNext());
            }
        }

        internal static Contact GetContact(Net.Uri contactUri)
        {
            if (contactUri == null)
                return default;

            using var context = Platform.AppContext.ContentResolver;
            using var cursor = context.Query(contactUri, null, null, null, null);

            if (cursor.MoveToFirst())
            {
                return GetContact(
                    cursor,
                    context,
                    ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId);
            }

            return default;
        }

        static Contact GetContact(ICursor cursor, ContentResolver context, string idKey)
        {
            var name = cursor.GetString(cursor.GetColumnIndex(ContactsContract.Contacts.InterfaceConsts.DisplayName));
            var idQ = new string[1] { cursor.GetString(cursor.GetColumnIndex(idKey)) };
            var phones = GetNumbers(context, idQ)?.Select(
                item => new ContactPhone(item.data, GetPhoneContactType(item.type)));
            var emails = GetEmails(context, idQ)?.Select(
                item => new ContactEmail(item.data, GetEmailContactType(item.type)));

            return new Contact(name, phones, emails);
        }

        static IEnumerable<(string data, string type)> GetNumbers(ContentResolver context, string[] idQ)
        {
            var uri = ContactsContract.CommonDataKinds.Phone.ContentUri.BuildUpon().AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1").Build();
            var cursor = context.Query(uri, null, $"{ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId}=?", idQ, null);

            return ReadCursorItems(cursor, ContactsContract.CommonDataKinds.Phone.Number, ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type);
        }

        static IEnumerable<(string data, string type)> GetEmails(ContentResolver context, string[] idQ)
        {
            var uri = ContactsContract.CommonDataKinds.Email.ContentUri.BuildUpon().AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1").Build();
            var cursor = context.Query(uri, null, $"{ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId}=?", idQ, null);

            return ReadCursorItems(cursor, ContactsContract.CommonDataKinds.Email.Address, ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type);
        }

        static IEnumerable<(string data, string type)> ReadCursorItems(ICursor cursor, string dataKey, string typeKey)
        {
            if (cursor?.MoveToFirst() ?? false)
            {
                do
                {
                    var data = cursor.GetString(cursor.GetColumnIndex(dataKey));
                    var type = cursor.GetString(cursor.GetColumnIndex(typeKey));

                    if (data != null)
                        yield return (data, type);
                }
                while (cursor.MoveToNext());
            }
            cursor?.Close();
        }

        static ContactType GetPhoneContactType(string type)
        {
            if (int.TryParse(type, out var typeInt))
            {
                try
                {
                    return (PhoneDataKind)typeInt switch
                    {
                        PhoneDataKind.Main => ContactType.Personal,
                        PhoneDataKind.Home => ContactType.Personal,
                        PhoneDataKind.Mobile => ContactType.Personal,
                        PhoneDataKind.Work => ContactType.Work,
                        PhoneDataKind.WorkMobile => ContactType.Work,
                        PhoneDataKind.CompanyMain => ContactType.Work,
                        PhoneDataKind.WorkPager => ContactType.Work,
                        _ => ContactType.Unknown
                    };
                }
                catch (Exception)
                {
                    return ContactType.Unknown;
                }
            }
            return ContactType.Unknown;
        }

        static ContactType GetEmailContactType(string type)
        {
            if (int.TryParse(type, out var typeInt))
            {
                try
                {
                    return (EmailDataKind)typeInt switch
                    {
                        EmailDataKind.Home => ContactType.Personal,
                        EmailDataKind.Work => ContactType.Work,
                        _ => ContactType.Unknown
                    };
                }
                catch (Exception)
                {
                    return ContactType.Unknown;
                }
            }
            return ContactType.Unknown;
        }
    }

    public partial class ContactsStore
    {
        public IEnumerable<Contact> GetAllPlatform(CancellationToken cancellationToken = default)
        {
            return null;
        }
    }
}
