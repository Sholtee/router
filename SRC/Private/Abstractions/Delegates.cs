/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Router.Internals
{
    internal delegate ref TData LookupDelegate<TData>(TData[] ar, string name);

    internal delegate StaticDictionary StaticDictionaryFactory();
}
