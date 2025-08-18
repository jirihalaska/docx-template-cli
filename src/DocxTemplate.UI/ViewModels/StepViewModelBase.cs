using System;
using System.Threading.Tasks;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// Base class for wizard step ViewModels
/// </summary>
public abstract class StepViewModelBase : ViewModelBase, IStepValidator
{
    private bool _isValid;
    private string? _validationError;

    /// <summary>
    /// Indicates whether the current step is valid and can proceed to next step
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    /// <summary>
    /// Error message to display if the step is not valid (IStepValidator interface)
    /// </summary>
    public string? ValidationError
    {
        get => _validationError;
        protected set => this.RaiseAndSetIfChanged(ref _validationError, value);
    }

    /// <summary>
    /// Error message to display if the step is not valid (backward compatibility)
    /// </summary>
    public string ErrorMessage
    {
        get => _validationError ?? string.Empty;
        protected set => ValidationError = value;
    }

    /// <summary>
    /// Validates the current step and updates IsValid and ValidationError properties
    /// </summary>
    /// <returns>True if the step is valid</returns>
    public abstract bool ValidateStep();

    /// <summary>
    /// Validates the current step asynchronously (IStepValidator interface)
    /// </summary>
    /// <returns>True if the step is valid</returns>
    public virtual Task<bool> ValidateAsync()
    {
        var result = ValidateStep();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Called when the step is activated/navigated to
    /// </summary>
    public virtual void OnStepActivated()
    {
        ValidateStep();
    }

    /// <summary>
    /// Called when the step is being left/navigated away from
    /// </summary>
    public virtual void OnStepDeactivated()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Updates validation state and raises property changed notifications
    /// </summary>
    protected void UpdateValidation()
    {
        // If we're already on UI thread, execute directly
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            var wasValid = IsValid;
            var isNowValid = ValidateStep();
            
            if (wasValid != isNowValid)
            {
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }
        else
        {
            // Post to UI thread if we're on a different thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var wasValid = IsValid;
                var isNowValid = ValidateStep();
                
                if (wasValid != isNowValid)
                {
                    this.RaisePropertyChanged(nameof(IsValid));
                }
            });
        }
    }
}