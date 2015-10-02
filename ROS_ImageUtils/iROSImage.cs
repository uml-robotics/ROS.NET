using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ROS_ImageWPF
{
    public interface iROSImage
    {
        GenericImage getGenericImage();
        void Desubscribe();
        void Resubscribe();
        bool IsSubscribed();
    }
}
