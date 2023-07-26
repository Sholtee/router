/********************************************************************************
* IsExternalInit.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

//
// On pre NET5_0 runtimes using C# 9 init accessor requires this hack:
// https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
// But in this case we have to define this class for all the TFMs:
// https://twitter.com/aarnott/status/1362786409954766858
//

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}
