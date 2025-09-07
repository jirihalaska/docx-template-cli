using DocxTemplate.Processing.Models;
using System.Text.RegularExpressions;

namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Exception thrown when a placeholder pattern is malformed or invalid
/// </summary>
public class InvalidPlaceholderPatternException : DocxTemplateException
{
    /// <summary>
    /// Initializes a new instance of the InvalidPlaceholderPatternException class
    /// </summary>
    public InvalidPlaceholderPatternException() : base("Invalid placeholder pattern")
    {
    }

    /// <summary>
    /// The invalid pattern that caused the exception
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    /// The position in the pattern where the error was detected (if available)
    /// </summary>
    public int? ErrorPosition { get; }

    /// <summary>
    /// Initializes a new instance of the InvalidPlaceholderPatternException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public InvalidPlaceholderPatternException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPlaceholderPatternException class with a pattern
    /// </summary>
    /// <param name="pattern">The invalid pattern</param>
    /// <param name="reason">Reason why the pattern is invalid</param>
    public InvalidPlaceholderPatternException(string pattern, string reason) 
        : base($"Invalid placeholder pattern '{pattern}': {reason}")
    {
        Pattern = pattern;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPlaceholderPatternException class with pattern and inner exception
    /// </summary>
    /// <param name="pattern">The invalid pattern</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public InvalidPlaceholderPatternException(string pattern, Exception innerException) 
        : base($"Invalid placeholder pattern '{pattern}': {innerException.Message}", innerException)
    {
        Pattern = pattern;
        
        // Extract error position from regex exception if available
        if (innerException is ArgumentException argEx && argEx.Message.Contains("parsing") && argEx.Message.Contains("at offset"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(argEx.Message, @"at offset (\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int position))
            {
                ErrorPosition = position;
            }
        }
    }

    /// <summary>
    /// Creates an InvalidPlaceholderPatternException for an empty pattern
    /// </summary>
    /// <returns>InvalidPlaceholderPatternException instance</returns>
    public static InvalidPlaceholderPatternException EmptyPattern()
    {
        return new InvalidPlaceholderPatternException("", "Pattern cannot be empty or whitespace");
    }

    /// <summary>
    /// Creates an InvalidPlaceholderPatternException for a pattern that's too long
    /// </summary>
    /// <param name="pattern">The pattern that's too long</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>InvalidPlaceholderPatternException instance</returns>
    public static InvalidPlaceholderPatternException PatternTooLong(string pattern, int maxLength)
    {
        return new InvalidPlaceholderPatternException(pattern, $"Pattern length ({pattern.Length}) exceeds maximum allowed length ({maxLength})");
    }

    /// <summary>
    /// Creates an InvalidPlaceholderPatternException for a regex compilation failure
    /// </summary>
    /// <param name="pattern">The pattern that failed to compile</param>
    /// <param name="regexException">The regex exception that occurred</param>
    /// <returns>InvalidPlaceholderPatternException instance</returns>
    public static InvalidPlaceholderPatternException RegexCompilationFailed(string pattern, Exception regexException)
    {
        return new InvalidPlaceholderPatternException(pattern, regexException);
    }

    /// <summary>
    /// Creates an InvalidPlaceholderPatternException for a pattern with dangerous constructs
    /// </summary>
    /// <param name="pattern">The dangerous pattern</param>
    /// <param name="dangerousConstruct">Description of the dangerous construct</param>
    /// <returns>InvalidPlaceholderPatternException instance</returns>
    public static InvalidPlaceholderPatternException DangerousPattern(string pattern, string dangerousConstruct)
    {
        return new InvalidPlaceholderPatternException(pattern, $"Pattern contains dangerous construct: {dangerousConstruct}");
    }

    /// <summary>
    /// Validates a placeholder pattern and throws an exception if invalid
    /// </summary>
    /// <param name="pattern">Pattern to validate</param>
    /// <param name="maxLength">Maximum allowed pattern length</param>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    public static void ValidatePattern(string pattern, int maxLength = 1000)
    {
        // Check for null or empty
        if (string.IsNullOrWhiteSpace(pattern))
            throw EmptyPattern();

        // Check length
        if (pattern.Length > maxLength)
            throw PatternTooLong(pattern, maxLength);

        // Check for dangerous constructs
        if (pattern.Contains("(?#") || pattern.Contains("(?>"))
            throw DangerousPattern(pattern, "Atomic groups or comments are not allowed");

        // Try to compile the regex
        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException ex)
        {
            throw RegexCompilationFailed(pattern, ex);
        }
        catch (RegexMatchTimeoutException)
        {
            throw DangerousPattern(pattern, "Pattern may cause catastrophic backtracking");
        }
    }
}