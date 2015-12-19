using System;

namespace Cat.Cat.Interfaces
{
    public interface ICatReturnFormat
    {
        string Format(object[] args);
    }

    /*! \interface  ICatReturnFormat
     *  \brief      IDL for the various CAT command return value formatters.
     */
}
