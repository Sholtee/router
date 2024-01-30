/********************************************************************************
* IElementAccessByInternalId.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    /// <summary>
    /// Provides quick access to elements stored in a <see cref="StaticDictionary"/>.
    /// </summary>
    internal interface IElementAccessByInternalId
    {
        /// <summary>
        /// Sets the element by its <paramref name="internalId"/>. You can grab internal ids when calling the <see cref="StaticDictionaryBuilder.CreateFactory(DelegateCompiler, out IReadOnlyDictionary{string, int})"/> method.
        /// </summary>
        void SetElementByInternalId(int internalId, object? value);

        /// <summary>
        /// Gets the element by its <paramref name="internalId"/>. You can grab internal ids when calling the <see cref="StaticDictionaryBuilder.CreateFactory(DelegateCompiler, out IReadOnlyDictionary{string, int})"/> method.
        /// </summary>
        object? GetElementByInternalId(int internalId);
    }
}
