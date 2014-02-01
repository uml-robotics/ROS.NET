using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DynamicReconfigureSharp
{
    public interface IDynamicReconfigureLayout
    {
        double getDescriptionWidth();
        void setDescriptionWidth(double w);
    }
}
