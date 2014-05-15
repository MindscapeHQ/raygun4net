using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    private string _machineName;
    private string _version;
    private RaygunErrorMessage _error;
    private RaygunEnvironmentMessage _environment;
    private RaygunClientMessage _client;
    private IList<string> _tags;
    private IDictionary _userCustomData;
    private RaygunIdentifierMessage _user;

    public string MachineName
    {
      get { return _machineName; }
      set
      {
        _machineName = value;
      }
    }

    public string Version
    {
      get { return _version; }
      set
      {
        _version = value;
      }
    }

    public RaygunErrorMessage Error
    {
      get { return _error; }
      set
      {
        _error = value;
      }
    }

    public RaygunEnvironmentMessage Environment
    {
      get { return _environment; }
      set
      {
        _environment = value;
      }
    }

    public RaygunClientMessage Client
    {
      get { return _client; }
      set
      {
        _client = value;
      }
    }

    public IList<string> Tags
    {
      get { return _tags; }
      set
      {
        _tags = value;
      }
    }

    public IDictionary UserCustomData
    {
      get { return _userCustomData; }
      set
      {
        _userCustomData = value;
      }
    }

    public RaygunIdentifierMessage User
    {
      get { return _user; }
      set
      {
        _user = value;
      }
    }
  }
}
