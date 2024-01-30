/********************************************************************************
* IElementAccessByInternalId.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Router
{
    /// <summary>
    /// Provides quick access to elements by id rather than name.
    /// </summary>
    public interface IElementAccessByInternalId
    {
        /// <summary>
        /// Sets the element by its <paramref name="internalId"/>.
        /// </summary>
        void SetElementByInternalId(int internalId, object? value);

        /// <summary>
        /// Gets the element by its <paramref name="internalId"/>.
        /// </summary>
        object? GetElementByInternalId(int internalId);
    }
}
