/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router.Internals
{
    internal delegate ref TData LookupDelegate<TData>(TData[] ar, ReadOnlySpan<char> name);

    internal delegate StaticDictionary StaticDictionaryFactory();
}
