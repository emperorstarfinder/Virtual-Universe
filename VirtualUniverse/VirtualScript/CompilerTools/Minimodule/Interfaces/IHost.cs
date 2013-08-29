﻿/*
 * Software license for this code will be added after the code is completed.
 * This code is in the early stages of development so should not be utilized 
 * at this time.
 * 
 * Thanks
 * Virtual Universe Development Team
 */

namespace VirtualUniverse.ScriptEngine.VirtualScript.MiniModule
{
    public interface IHost
    {
        IObject Object { get; }
        IGraphics Graphics { get; }
        IExtension Extensions { get; }
        IMicrothreader Microthreads { get; }
    }
}