using System;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Services.EmailTemplateModel;

namespace JLSApplicationBackend.Services;

/// <summary>
/// Converts a strongly-typed <see cref="EmailTemplateContext"/> into a rendered HTML string.
/// Implementations can use Razor views, Fluid/Scriban templates, or any other strategy —
/// callers do not need to know which approach is used.
/// </summary>
public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(EmailTemplateContext context);
}



/// <summary>
/// Renders email templates using the lightweight Scriban framework instead of the heavy MVC Razor engine.
/// Reads .html files directly from the specified template directory.
/// </summary>
public class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly string _templateDirectory;

    public ScribanEmailTemplateRenderer(string templateDirectory)
    {
        _templateDirectory = templateDirectory;
    }

    public async Task<string> RenderAsync(EmailTemplateContext context)
    {
        var (viewName, title, model) = context switch
        {
            NewOrderClientContext c => ("new-order-client", "Votre commande a été passée", (object)new { c.Username, c.OrderNumber }),
            NewOrderAdminContext c => ("new-order-admin", "Nouvelle commande est arrivée", (object)new { c.OrderNumber }),
            ModifyOrderClientContext c => ("modify-order-client", "Votre commande est bien modifiée", (object)new { c.Username, c.OrderNumber }),
            ModifyOrderAdminContext c => ("modify-order-admin", "Nouvelle commande a été modifié", (object)new { c.Username, c.OrderNumber }),
            EmailConfirmationContext c => ("email-confirmation", "Félicitation! Vous avez bien créé votre compte", (object)new { c.Username, c.ConfirmationLink, c.Entreprise, c.Phone }),
            ResetPasswordContext c => ("reset-password", "Initialiser votre mot de passe", (object)new { c.Username, c.ConfirmationLink, c.Entreprise, c.Phone }),
            AfterEmailConfirmationContext => ("after-email-confirmation", "Votre compte est bien activée.", (object)new { }),
            AfterResetPasswordContext => ("after-reset-password", "Votre mot de passe est bien réinitialiser.", (object)new { }),
            ClientMessageToAdminContext c => ("client-message-to-admin", "Nouveau message", (object)new { Email = c.Email, c.Message }),
            _ => throw new ArgumentOutOfRangeException(nameof(context), $"Unknown email template context: {context.GetType().Name}")
        };

        // 1. Render the inner view with the model
        var viewPath = Path.Combine(_templateDirectory, $"{viewName}.html");
        var viewTemplateStr = await File.ReadAllTextAsync(viewPath);
        var viewTemplate = Scriban.Template.Parse(viewTemplateStr, viewPath);
        var bodyHtml = await viewTemplate.RenderAsync(model);

        // 2. Wrap the rendered view in the _layout.html
        var layoutPath = Path.Combine(_templateDirectory, "_layout.html");
        var layoutTemplateStr = await File.ReadAllTextAsync(layoutPath);
        var layoutTemplate = Scriban.Template.Parse(layoutTemplateStr, layoutPath);

        // Pass 'title' and 'body' to the layout
        var scriptObject = new Scriban.Runtime.ScriptObject
        {
            { "title", title },
            { "body", bodyHtml }
        };
        var templateContext = new Scriban.TemplateContext();
        templateContext.PushGlobal(scriptObject);

        return await layoutTemplate.RenderAsync(templateContext);
    }
}

