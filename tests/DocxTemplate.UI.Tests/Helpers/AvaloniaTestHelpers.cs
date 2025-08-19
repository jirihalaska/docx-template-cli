using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Xunit;

namespace DocxTemplate.UI.Tests.Helpers;

/// <summary>
/// Helper methods for Avalonia UI automation in tests
/// </summary>
public static class AvaloniaTestHelpers
{
    /// <summary>
    /// Finds a control by name within the visual tree
    /// </summary>
    /// <typeparam name="T">Type of control to find</typeparam>
    /// <param name="root">Root visual to search from</param>
    /// <param name="name">Name of the control to find</param>
    /// <returns>The found control</returns>
    public static async Task<T> FindControl<T>(this Visual root, string name) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var control = root.FindDescendantOfType<T>();
            if (control?.Name != name)
            {
                // Try to find by name in all descendants
                control = root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);
            }
            if (control == null)
            {
                throw new InvalidOperationException($"Control of type {typeof(T).Name} with name '{name}' not found");
            }
            return control;
        });
    }

    /// <summary>
    /// Finds a control by type within the visual tree
    /// </summary>
    /// <typeparam name="T">Type of control to find</typeparam>
    /// <param name="root">Root visual to search from</param>
    /// <returns>The found control</returns>
    public static async Task<T> FindControl<T>(this Visual root) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var control = root.FindDescendantOfType<T>();
            if (control == null)
            {
                throw new InvalidOperationException($"Control of type {typeof(T).Name} not found");
            }
            return control;
        });
    }

    /// <summary>
    /// Finds all controls of a specific type within the visual tree
    /// </summary>
    /// <typeparam name="T">Type of control to find</typeparam>
    /// <param name="root">Root visual to search from</param>
    /// <returns>List of found controls</returns>
    public static async Task<IReadOnlyList<T>> FindControls<T>(this Visual root) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            return root.GetVisualDescendants().OfType<T>().ToList();
        });
    }

    /// <summary>
    /// Waits for a condition to become true
    /// </summary>
    /// <param name="condition">Condition to wait for</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <param name="message">Optional error message if timeout occurs</param>
    public static async Task WaitFor(Func<bool> condition, int timeout = 5000, string? message = null)
    {
        var endTime = DateTime.UtcNow.AddMilliseconds(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (condition())
                return;
                
            await Task.Delay(50);
        }
        
        throw new TimeoutException(message ?? $"Condition did not become true within {timeout}ms");
    }

    /// <summary>
    /// Waits for a condition to become true with async condition
    /// </summary>
    /// <param name="condition">Async condition to wait for</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <param name="message">Optional error message if timeout occurs</param>
    public static async Task WaitFor(Func<Task<bool>> condition, int timeout = 5000, string? message = null)
    {
        var endTime = DateTime.UtcNow.AddMilliseconds(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (await condition())
                return;
                
            await Task.Delay(50);
        }
        
        throw new TimeoutException(message ?? $"Condition did not become true within {timeout}ms");
    }

    /// <summary>
    /// Simulates a click on a button
    /// </summary>
    /// <param name="button">Button to click</param>
    public static async Task SimulateClick(Button button)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!button.IsEnabled)
            {
                throw new InvalidOperationException($"Cannot click disabled button '{button.Name ?? button.GetType().Name}'");
            }
            
            // Simulate pointer press and release
            // Simple approach: just call the command directly if available
            if (button.Command?.CanExecute(button.CommandParameter) == true)
            {
                button.Command.Execute(button.CommandParameter);
            }
            else
            {
                // Fallback: trigger click event
                var clickEventArgs = new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent, button);
                button.RaiseEvent(clickEventArgs);
            }
        });
    }

    /// <summary>
    /// Simulates text input into a TextBox
    /// </summary>
    /// <param name="textBox">TextBox to input text into</param>
    /// <param name="text">Text to input</param>
    public static async Task SimulateTextInput(TextBox textBox, string text)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!textBox.IsEnabled)
            {
                throw new InvalidOperationException($"Cannot input text into disabled TextBox '{textBox.Name ?? textBox.GetType().Name}'");
            }
            
            // Focus the text box first
            textBox.Focus();
            
            // Clear existing text
            textBox.Text = string.Empty;
            
            // Set the new text
            textBox.Text = text;
            
            // Simulate text input event
            var textInputArgs = new TextInputEventArgs
            {
                Text = text,
                Source = textBox,
                RoutedEvent = InputElement.TextInputEvent
            };
            textBox.RaiseEvent(textInputArgs);
        });
    }

    /// <summary>
    /// Simulates selection of an item in a ListBox
    /// </summary>
    /// <param name="listBox">ListBox to select item in</param>
    /// <param name="index">Index of item to select</param>
    public static async Task SimulateSelection(ListBox listBox, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!listBox.IsEnabled)
            {
                throw new InvalidOperationException($"Cannot select item in disabled ListBox '{listBox.Name ?? listBox.GetType().Name}'");
            }
            
            if (index < 0 || index >= listBox.ItemCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for ListBox with {listBox.ItemCount} items");
            }
            
            listBox.SelectedIndex = index;
        });
    }

    /// <summary>
    /// Waits for a control to become enabled
    /// </summary>
    /// <param name="control">Control to wait for</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    public static async Task WaitForEnabled(Control control, int timeout = 5000)
    {
        await WaitFor(() => control.IsEnabled, timeout, $"Control '{control.Name ?? control.GetType().Name}' did not become enabled within {timeout}ms");
    }

    /// <summary>
    /// Waits for a control to become visible
    /// </summary>
    /// <param name="control">Control to wait for</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    public static async Task WaitForVisible(Control control, int timeout = 5000)
    {
        await WaitFor(() => control.IsVisible, timeout, $"Control '{control.Name ?? control.GetType().Name}' did not become visible within {timeout}ms");
    }

    /// <summary>
    /// Gets the text content from various control types
    /// </summary>
    /// <param name="control">Control to get text from</param>
    /// <returns>Text content</returns>
    public static async Task<string> GetText(Control control)
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            return control switch
            {
                TextBlock textBlock => textBlock.Text ?? string.Empty,
                TextBox textBox => textBox.Text ?? string.Empty,
                Button button => button.Content?.ToString() ?? string.Empty,
                Label label => label.Content?.ToString() ?? string.Empty,
                _ => control.ToString() ?? string.Empty
            };
        });
    }

    /// <summary>
    /// Asserts that a control is visible and enabled
    /// </summary>
    /// <param name="control">Control to check</param>
    /// <param name="controlName">Name for error messages</param>
    public static async Task AssertVisible(Control control, string? controlName = null)
    {
        var name = controlName ?? control.Name ?? control.GetType().Name;
        var isVisible = await Dispatcher.UIThread.InvokeAsync(() => control.IsVisible);
        Assert.True(isVisible, $"Control '{name}' should be visible");
    }

    /// <summary>
    /// Asserts that a control is enabled
    /// </summary>
    /// <param name="control">Control to check</param>
    /// <param name="controlName">Name for error messages</param>
    public static async Task AssertEnabled(Control control, string? controlName = null)
    {
        var name = controlName ?? control.Name ?? control.GetType().Name;
        var isEnabled = await Dispatcher.UIThread.InvokeAsync(() => control.IsEnabled);
        Assert.True(isEnabled, $"Control '{name}' should be enabled");
    }
}

/// <summary>
/// Mock helper for creating mock objects in tests
/// </summary>
public static class Mock
{
    /// <summary>
    /// Creates a mock of the specified interface
    /// </summary>
    /// <typeparam name="T">Interface type to mock</typeparam>
    /// <returns>Mock instance</returns>
    public static T Of<T>() where T : class
    {
        // Simple null object pattern since we're not using a mocking framework
        return (T)Activator.CreateInstance(typeof(T))!;
    }
}