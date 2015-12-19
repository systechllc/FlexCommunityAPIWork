using System;
using System.Reflection;
using Cat.Cat;
using Cat.Cat.Ports;
//using Cat.Clients;
using Cat.Cfg;
using FabulaTech.VSPK;
using log4net;


namespace Cat
{
    internal sealed class ObjectManager
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		static readonly ObjectManager instance = new ObjectManager();

        internal Factory Factory { get; private set; }
        internal CatCmdProcessor CatCmdProcessor  { get; private set; }
        internal SerialPortManager SerialPortManager { get; private set; }
        internal FTVSPKControl vspkControl { get; private set; }
		//internal CatCache CatCache { get; private set; }
		internal CatStateManager StateManager { get; private set; }
		internal DataManager DataManager {get; private set;}
		internal TcpPortManager TcpPortManager { get; private set; }
  
        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static ObjectManager()
        {
        }

        // public property that can only get the single instance of this class.
        public static ObjectManager Instance
        {
            get { return instance; }
        }

        // make the default constructor private, so that no one can directly create it.
        private ObjectManager()
        {
            try
            {
                Factory = new Factory();
                CatCmdProcessor = new CatCmdProcessor(this);
                SerialPortManager = new SerialPortManager(this);
                vspkControl = new FTVSPKControl();
                vspkControl.Enable(true);
				//CatCache = new CatCache();
				StateManager = new CatStateManager();
				DataManager = new DataManager();
				TcpPortManager = new TcpPortManager(this);
              }
            catch (Exception ex)
            {
                _log.Fatal(ex.Message);
            }
        }

        /*! \class Cat.ObjectManager
         * \brief       Holds instances of universally required objects.
         * \author      Bob Tracy, K5KDN
         * \version     0.1Alpha
         * \date        01/2012
         * \copyright   FlexRadio Systems
         */


    }
}
