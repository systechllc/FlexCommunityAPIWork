using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace Cat.Cat.Interfaces
{
    public interface ISerialComSpec
    {
        int baudRate { get; set; }
        int dataBits { get; set; }
        Parity parity { get; set; }
        StopBits stopBits { get; set; }
    }

    /*! \interface  ISerialComSpec
     *  \brief      IDL for CAT serial communication parameters.
     */
}
