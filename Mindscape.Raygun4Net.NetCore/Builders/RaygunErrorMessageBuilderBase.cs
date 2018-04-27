using System;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public abstract class RaygunErrorMessageBuilderBase
  {
    protected static string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      if (!type.IsConstructedGenericType)
      {
        return name;
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      foreach (Type t in type.GenericTypeArguments)
      {
        stringBuilder.Append(FormatTypeName(t, false)).Append(",");
      }
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }
    
    protected static string GenerateMethodName(MethodBase method)
    {
      var stringBuilder = new StringBuilder();

      stringBuilder.Append(method.Name);

      bool first = true;
      
      if (method is MethodInfo && method.IsGenericMethod)
      {
        Type[] genericArguments = method.GetGenericArguments();
        stringBuilder.Append("[");
        
        for (int i = 0; i < genericArguments.Length; i++)
        {
          if (!first)
          {
            stringBuilder.Append(",");
          }
          else
          {
            first = false;
          }
          
          stringBuilder.Append(genericArguments[i].Name);
        }
        
        stringBuilder.Append("]");
      }
      
      stringBuilder.Append("(");
      
      ParameterInfo[] parameters = method.GetParameters();
      
      first = true;
      
      for (int i = 0; i < parameters.Length; ++i)
      {
        if (!first)
        {
          stringBuilder.Append(", ");
        }
        else
        {
          first = false;
        }
        
        string type = "<UnknownType>";
        
        if (parameters[i].ParameterType != null)
        {
          type = parameters[i].ParameterType.Name;
        }
        
        stringBuilder.Append(type + " " + parameters[i].Name);
      }
      
      stringBuilder.Append(")");

      return stringBuilder.ToString();
    }
  }
}
