﻿using System;

namespace Mindscape.Raygun4Net
{
  /// <summary>
  /// Can be used to create a custom grouping key for exceptions
  /// </summary>
  public class RaygunCustomGroupingKeyEventArgs : EventArgs
  {
    public RaygunCustomGroupingKeyEventArgs(Exception exception, RaygunMessage message)
    {
      Exception = exception;
      Message = message;
    }
    
    public Exception Exception { get; private set; }
    
    public RaygunMessage Message { get; private set; }

    public string CustomGroupingKey { get; set; }    
  }
}