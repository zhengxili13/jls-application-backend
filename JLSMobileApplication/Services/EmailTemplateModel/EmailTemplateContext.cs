namespace JLSApplicationBackend.Services.EmailTemplateModel;

/// <summary>
/// Identifies a named email template scenario. Passed to IEmailTemplateRenderer
/// to resolve the correct Razor view + model without hard-coding view paths in
/// business logic.
/// </summary>
public abstract record EmailTemplateContext;

// --- Order emails ---
public record NewOrderClientContext(string Username, string OrderNumber) : EmailTemplateContext;
public record NewOrderAdminContext(string Username, string OrderNumber) : EmailTemplateContext;
public record ModifyOrderClientContext(string Username, string OrderNumber) : EmailTemplateContext;
public record ModifyOrderAdminContext(string Username, string OrderNumber) : EmailTemplateContext;

// --- Account emails ---
public record EmailConfirmationContext(string Username, string ConfirmationLink, string Entreprise, string Phone) : EmailTemplateContext;
public record ResetPasswordContext(string Username, string ConfirmationLink, string Entreprise, string Phone) : EmailTemplateContext;
public record AfterEmailConfirmationContext : EmailTemplateContext;
public record AfterResetPasswordContext : EmailTemplateContext;

// --- Contact ---
public record ClientMessageToAdminContext(string Email, string Message) : EmailTemplateContext;
