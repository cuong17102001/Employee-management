﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tc.BaseLibrary.Responses
{
    public record GeneralResponse(bool flag, string message = null!);
}
