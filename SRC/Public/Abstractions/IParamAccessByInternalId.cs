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
    public interface IParamAccessByInternalId<TData>
    {
        /// <summary>
        /// Gets the element by its <paramref name="internalId"/>.
        /// </summary>
        TData? this[int internalId]
        {
            get; internal set;
        }
    }
}
