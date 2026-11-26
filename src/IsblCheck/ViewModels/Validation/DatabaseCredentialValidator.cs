using FluentValidation;
using IsblCheck.Common.Localization;
using IsblCheck.ViewModels.Dialogs;

namespace IsblCheck.ViewModels.Validation
{
  /// <summary>
  /// Валидатор данных формы подключения к бд.
  /// </summary>
  public class DatabaseCredentialValidator : AbstractValidator<DatabaseCredentialViewModel>
  {
    private readonly bool needCheckDatabase;

    public DatabaseCredentialValidator(bool needCheckDatabase = true)
    {
      this.needCheckDatabase = needCheckDatabase;
      this.RuleFor(d => d.DatabaseSource)
        .NotEmpty()
        .WithMessage(LocalizationManager.Instance.LocalizeString("FIELD_COULD_NOT_BE_EMPTY"));
      this.RuleFor(d => d.UserName)
        .NotEmpty()
        .When(d => !d.IntegratedSecurity)
        .WithMessage(LocalizationManager.Instance.LocalizeString("FIELD_COULD_NOT_BE_EMPTY"));
      if (this.needCheckDatabase)
      {
        this.RuleFor(d => d.Database)
          .NotEmpty()
          .WithMessage(LocalizationManager.Instance.LocalizeString("FIELD_COULD_NOT_BE_EMPTY"));
      }
    }

  }
}
