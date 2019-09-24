﻿using System;

namespace Mindscape.Raygun4Net.AspNetCore2.Tests.TestLib
{
    public class TestLib
    {
        public void TestLibSendErrorUsingDotNetCore()
        {
            try
            {
                throw new LibException();
            }
            catch (Exception e)
            {
                RaygunClientFactory.GetClient().Send(e);
            }
            
        }
    }

    public class LibException : Exception
    {
        
    }
}