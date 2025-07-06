namespace MicroAPI.Sample;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

public class RequireExplicitHttpMethodConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            if (action.Selectors.Any(s => s.AttributeRouteModel != null || s.ActionConstraints?.Any() == true))
            {
                continue;
            }
            action.ApiExplorer.IsVisible = false;
            action.Selectors.Clear();
        }
    }
}