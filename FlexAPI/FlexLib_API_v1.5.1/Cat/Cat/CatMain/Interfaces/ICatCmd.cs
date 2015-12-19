using System;


namespace Cat.Cat.Interfaces
{
    public interface ICatCmd
    {
        object Execute(string cmd);
    }

    /*! \interface  ICatCmd
     *  \brief      IDL for CAT commands.
     */
}
