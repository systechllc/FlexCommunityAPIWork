using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace Cat.Cat.Interfaces
{
    interface ISerialPort
    {
        void Write(string data);
    }

    /*! \interface  ISerialPort
     *  \brief      IDL for CAT serial ports.
     */
}
