// ****************************************************************************
///*!	\file VisualStudio.cs
// *	\brief Utility functions useful in Visual Studio
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2012-03-05
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

using System.ComponentModel;

namespace Flex.UiWpfFramework.Utils
{
    public static class VisualStudio
    {
        public static bool IsDesignTime
        {
            get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
        }
    }
}
