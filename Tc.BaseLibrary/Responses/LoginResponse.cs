using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tc.BaseLibrary.Responses
{
    public record LoginResponse(bool flag, string message = null!, string token = null!, string refreshToken = null!);
}
