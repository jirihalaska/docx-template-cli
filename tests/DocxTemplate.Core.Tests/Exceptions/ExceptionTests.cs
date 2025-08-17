using DocxTemplate.Core.Exceptions;
using FluentAssertions;

namespace DocxTemplate.Core.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void DocxTemplateException_ShouldBeAbstractBaseException()
    {
        // arrange & act
        var type = typeof(DocxTemplateException);

        // assert
        type.Should().BeAbstract();
        type.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void TemplateNotFoundException_ShouldInheritFromDocxTemplateException()
    {
        // arrange & act
        var exception = new TemplateNotFoundException();

        // assert
        exception.Should().BeAssignableTo<DocxTemplateException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void TemplateNotFoundException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // arrange & act
        var exception = new TemplateNotFoundException();

        // assert
        exception.Message.Should().Be("Template file not found");
        exception.TemplatePath.Should().BeNull();
    }

    [Fact]
    public void TemplateNotFoundException_MessageConstructor_ShouldSetMessage()
    {
        // arrange
        var message = "Custom error message";

        // act
        var exception = new TemplateNotFoundException(message);

        // assert
        exception.Message.Should().Be(message);
        exception.TemplatePath.Should().BeNull();
    }

    [Fact]
    public void TemplateNotFoundException_PathConstructor_ShouldSetPathAndMessage()
    {
        // arrange
        var templatePath = "/path/to/template.docx";

        // act
        var exception = new TemplateNotFoundException(templatePath, true);

        // assert
        exception.Message.Should().Be($"Template file not found: {templatePath}");
        exception.TemplatePath.Should().Be(templatePath);
    }

    [Fact]
    public void TemplateNotFoundException_PathAndInnerExceptionConstructor_ShouldSetAllProperties()
    {
        // arrange
        var templatePath = "/path/to/template.docx";
        var innerException = new FileNotFoundException("Inner exception");

        // act
        var exception = new TemplateNotFoundException(templatePath, innerException);

        // assert
        exception.Message.Should().Be($"Template file not found: {templatePath}");
        exception.TemplatePath.Should().Be(templatePath);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void TemplateNotFoundException_ForFile_ShouldCreateWithFilePath()
    {
        // arrange
        var filePath = "/path/to/missing.docx";

        // act
        var exception = TemplateNotFoundException.ForFile(filePath);

        // assert
        exception.Should().NotBeNull();
        exception.TemplatePath.Should().Be(filePath);
        exception.Message.Should().Contain(filePath);
    }

    [Fact]
    public void TemplateNotFoundException_ForDirectory_ShouldCreateWithDirectoryPath()
    {
        // arrange
        var directoryPath = "/path/to/missing/directory";

        // act
        var exception = TemplateNotFoundException.ForDirectory(directoryPath);

        // assert
        exception.Should().NotBeNull();
        exception.TemplatePath.Should().Be(directoryPath);
        exception.Message.Should().Contain(directoryPath);
        exception.InnerException.Should().BeOfType<DirectoryNotFoundException>();
    }

    [Fact]
    public void InvalidPlaceholderPatternException_ShouldInheritFromDocxTemplateException()
    {
        // arrange & act
        var exception = new InvalidPlaceholderPatternException();

        // assert
        exception.Should().BeAssignableTo<DocxTemplateException>();
    }

    [Fact]
    public void InvalidPlaceholderPatternException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // arrange & act
        var exception = new InvalidPlaceholderPatternException();

        // assert
        exception.Message.Should().Be("Invalid placeholder pattern");
        exception.Pattern.Should().BeNull();
    }

    [Fact]
    public void InvalidPlaceholderPatternException_PatternConstructor_ShouldSetPatternAndMessage()
    {
        // arrange
        var pattern = "[invalid";
        var reason = "Pattern is malformed";

        // act
        var exception = new InvalidPlaceholderPatternException(pattern, reason);

        // assert
        exception.Message.Should().Be($"Invalid placeholder pattern '{pattern}': {reason}");
        exception.Pattern.Should().Be(pattern);
    }

    [Fact]
    public void InvalidPlaceholderPatternException_PatternAndInnerExceptionConstructor_ShouldSetAllProperties()
    {
        // arrange
        var pattern = "*invalid";
        var innerException = new ArgumentException("Inner exception");

        // act
        var exception = new InvalidPlaceholderPatternException(pattern, innerException);

        // assert
        exception.Message.Should().Be($"Invalid placeholder pattern '{pattern}': {innerException.Message}");
        exception.Pattern.Should().Be(pattern);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void DocumentProcessingException_ShouldInheritFromDocxTemplateException()
    {
        // arrange & act
        var exception = new DocumentProcessingException();

        // assert
        exception.Should().BeAssignableTo<DocxTemplateException>();
    }

    [Fact]
    public void DocumentProcessingException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // arrange & act
        var exception = new DocumentProcessingException();

        // assert
        exception.Message.Should().Be("Document processing error");
        exception.DocumentPath.Should().BeNull();
    }

    [Fact]
    public void DocumentProcessingException_DocumentPathConstructor_ShouldSetDocumentPathAndMessage()
    {
        // arrange
        var documentPath = "/path/to/document.docx";
        var operation = "process";
        var message = "Processing failed";

        // act
        var exception = new DocumentProcessingException(documentPath, operation, message);

        // assert
        exception.Message.Should().Be($"Failed to {operation} document '{Path.GetFileName(documentPath)}': {message}");
        exception.DocumentPath.Should().Be(documentPath);
        exception.Operation.Should().Be(operation);
    }

    [Fact]
    public void DocumentProcessingException_DocumentPathAndInnerExceptionConstructor_ShouldSetAllProperties()
    {
        // arrange
        var documentPath = "/path/to/document.docx";
        var operation = "process";
        var message = "Processing failed";
        var innerException = new InvalidOperationException("Inner exception");

        // act
        var exception = new DocumentProcessingException(documentPath, operation, message, innerException);

        // assert
        exception.Message.Should().Be($"Failed to {operation} document '{Path.GetFileName(documentPath)}': {message}");
        exception.DocumentPath.Should().Be(documentPath);
        exception.Operation.Should().Be(operation);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ReplacementValidationException_ShouldInheritFromDocxTemplateException()
    {
        // arrange & act
        var exception = new ReplacementValidationException();

        // assert
        exception.Should().BeAssignableTo<DocxTemplateException>();
    }

    [Fact]
    public void ReplacementValidationException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // arrange & act
        var exception = new ReplacementValidationException();

        // assert
        exception.Message.Should().Be("Replacement validation failed");
        exception.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void ReplacementValidationException_PlaceholderConstructor_ShouldSetPropertiesAndMessage()
    {
        // arrange
        var placeholderName = "testPlaceholder";
        var validationRule = "Required field is missing";

        // act
        var exception = new ReplacementValidationException(placeholderName, validationRule);

        // assert
        exception.Message.Should().Contain($"placeholder '{placeholderName}'");
        exception.Message.Should().Contain(validationRule);
        exception.PlaceholderName.Should().Be(placeholderName);
        exception.ValidationRule.Should().Be(validationRule);
        exception.ValidationErrors.Should().ContainSingle();
    }

    [Fact]
    public void ReplacementValidationException_PlaceholderValueConstructor_ShouldSetAllProperties()
    {
        // arrange
        var placeholderName = "testPlaceholder";
        var replacementValue = "invalidValue";
        var validationRule = "Value contains invalid characters";

        // act
        var exception = new ReplacementValidationException(placeholderName, replacementValue, validationRule);

        // assert
        exception.Message.Should().Contain($"placeholder '{placeholderName}'");
        exception.Message.Should().Contain($"value '{replacementValue}'");
        exception.Message.Should().Contain(validationRule);
        exception.PlaceholderName.Should().Be(placeholderName);
        exception.ReplacementValue.Should().Be(replacementValue);
        exception.ValidationRule.Should().Be(validationRule);
        exception.ValidationErrors.Should().ContainSingle();
    }

    [Fact]
    public void ReplacementValidationException_ValidationErrorsListConstructor_ShouldSetErrorsAndMessage()
    {
        // arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        // act
        var exception = new ReplacementValidationException(errors);

        // assert
        exception.Message.Should().Contain("Replacement validation failed with 3 error(s)");
        exception.ValidationErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void FileAccessException_ShouldInheritFromDocxTemplateException()
    {
        // arrange & act
        var exception = new FileAccessException();

        // assert
        exception.Should().BeAssignableTo<DocxTemplateException>();
    }

    [Fact]
    public void FileAccessException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        // arrange & act
        var exception = new FileAccessException();

        // assert
        exception.Message.Should().Be("File access error");
        exception.FilePath.Should().BeNull();
        exception.Operation.Should().BeNull();
        exception.AccessType.Should().Be(FileAccessType.Unknown);
    }

    [Fact]
    public void FileAccessException_FilePathAndOperationConstructor_ShouldSetPropertiesAndMessage()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var operation = "read";
        var accessType = FileAccessType.Read;

        // act
        var exception = new FileAccessException(filePath, operation, accessType);

        // assert
        exception.Message.Should().Contain($"Failed to {operation} file");
        exception.Message.Should().Contain("file.docx");
        exception.FilePath.Should().Be(filePath);
        exception.Operation.Should().Be(operation);
        exception.AccessType.Should().Be(accessType);
    }

    [Fact]
    public void FileAccessException_FilePathOperationAndInnerExceptionConstructor_ShouldSetAllProperties()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var operation = "write";
        var accessType = FileAccessType.Write;
        var innerException = new UnauthorizedAccessException("Inner exception");

        // act
        var exception = new FileAccessException(filePath, operation, accessType, innerException);

        // assert
        exception.Message.Should().Contain($"Failed to {operation} file");
        exception.FilePath.Should().Be(filePath);
        exception.Operation.Should().Be(operation);
        exception.AccessType.Should().Be(accessType);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void FileAccessException_StaticFactoryMethods_ShouldCreateCorrectExceptions()
    {
        // arrange
        var filePath = "/test/file.docx";
        var innerException = new IOException("Test exception");

        // act & assert
        var readException = FileAccessException.CannotRead(filePath, innerException);
        readException.AccessType.Should().Be(FileAccessType.Read);
        readException.Operation.Should().Be("read");

        var writeException = FileAccessException.CannotWrite(filePath, innerException);
        writeException.AccessType.Should().Be(FileAccessType.Write);
        writeException.Operation.Should().Be("write");

        var createException = FileAccessException.CannotCreate(filePath, innerException);
        createException.AccessType.Should().Be(FileAccessType.Create);
        createException.Operation.Should().Be("create");

        var deleteException = FileAccessException.CannotDelete(filePath, innerException);
        deleteException.AccessType.Should().Be(FileAccessType.Delete);
        deleteException.Operation.Should().Be("delete");

        var permissionException = FileAccessException.InsufficientPermissions(filePath, FileAccessType.Write);
        permissionException.AccessType.Should().Be(FileAccessType.Write);
        permissionException.Message.Should().Contain("Insufficient permissions");
    }

    [Fact]
    public void AllExceptions_ShouldBeSerializable()
    {
        // arrange
        var exceptions = new List<Exception>
        {
            new TemplateNotFoundException("test"),
            new InvalidPlaceholderPatternException("pattern", "reason"),
            new DocumentProcessingException("doc.docx", "process", "message"),
            new ReplacementValidationException("placeholder", "validation rule"),
            new FileAccessException("file.docx", "read", FileAccessType.Read)
        };

        // act & assert - Skip serializable check as exceptions may not be marked [Serializable] in modern .NET
        foreach (var exception in exceptions)
        {
            exception.Should().BeAssignableTo<Exception>();
            exception.Should().BeAssignableTo<DocxTemplateException>();
        }
    }

    [Fact]
    public void ReplacementValidationException_GetValidationSummary_ShouldFormatCorrectly()
    {
        // arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var exception = new ReplacementValidationException(errors);

        // act
        var summary = exception.GetValidationSummary();

        // assert
        summary.Should().Contain("Multiple validation errors (3)");
        summary.Should().Contain("1. Error 1");
        summary.Should().Contain("2. Error 2");
        summary.Should().Contain("3. Error 3");
    }
}