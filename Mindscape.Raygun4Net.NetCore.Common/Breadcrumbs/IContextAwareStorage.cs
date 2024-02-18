namespace Mindscape.Raygun4Net.Breadcrumbs;

public interface IContextAwareStorage : IRaygunBreadcrumbStorage
{
  public void BeginContext();

  public void EndContext();
}