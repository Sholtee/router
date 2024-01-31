/********************************************************************************
* IParamAccessByInternalId.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Router
{
    /// <summary>
    /// Provides quick access to parameters by id rather than name.
    /// </summary>
    public interface IParamAccessByInternalId
    {
        /// <summary>
        /// Gets the element by its <paramref name="internalId"/>.
        /// </summary>
        object? this[int internalId]
        {
            get; internal set;
        }
    }
}
