

namespace Cat.Cat
{
    public interface ICatStateMgr
    {
		string IFStatus { get; set; }
		string ZZIFStatus { get; set; }
        string Set(string Key, string Value);
        string Get(string Key);
		string Verify(string Key);
    }

    /*! \interface  ICatStateMgr
     *  \brief      IDL for the CAT internal state manager.
     */
}
