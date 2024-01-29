/********************************************************************************
* IElementAccessByInternalId.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Router.Internals
{
    internal interface IElementAccessByInternalId
    {
        void SetElementByInternalId(int internalId, object? value);
        object? GetElementByInternalId(int internalId);
    }
}
