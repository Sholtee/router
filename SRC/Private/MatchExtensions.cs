/********************************************************************************
* MatchExtensions.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text.RegularExpressions;

namespace Solti.Utils.Router.Internals
{
    internal static class MatchExtensions
    {
        public static string? GetGroup(this Match self, string name)
        {
            Group group = self.Groups[name];
            return group.Success ? group.Value : null;
        }
    }
}
