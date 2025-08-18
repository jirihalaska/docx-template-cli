using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.GUI.Infrastructure;

public class TestDataManager
{
    private readonly string _testDataDirectory;

    public TestDataManager(string testDataDirectory)
    {
        _testDataDirectory = testDataDirectory;
    }

    public async Task<string> CreateTestTemplateSetAsync(string templateSetName)
    {
        var templateSetDir = Path.Combine(_testDataDirectory, templateSetName);
        Directory.CreateDirectory(templateSetDir);

        // Create a simple test document with placeholders
        var docPath = Path.Combine(templateSetDir, "TestDocument.docx");
        await CreateTestDocumentAsync(docPath);

        return templateSetDir;
    }

    private async Task CreateTestDocumentAsync(string filePath)
    {
        using var document = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        
        // Add document structure
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Add content with placeholders
        var title = new Paragraph(new Run(new Text("Test Document Template")));
        title.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = "Title" }));
        body.AppendChild(title);

        var contentParagraphs = new[]
        {
            "Company: {{company_name}}",
            "Project: {{project_title}}",
            "Date: {{current_date}}",
            "Author: {{author_name}}",
            "",
            "This document contains {{company_name}} project details for {{project_title}}.",
            "Created by {{author_name}} on {{current_date}}."
        };

        foreach (var content in contentParagraphs)
        {
            var para = new Paragraph(new Run(new Text(content)));
            body.AppendChild(para);
        }

        await Task.CompletedTask;
    }

    public async Task<string> CreateTestPlaceholderValuesAsync(string fileName)
    {
        var values = new Dictionary<string, string>
        {
            { "company_name", "Test Company Ltd" },
            { "project_title", "E2E Integration Test Project" },
            { "current_date", "2025-08-18" },
            { "author_name", "E2E Test Suite" }
        };

        var filePath = Path.Combine(_testDataDirectory, fileName);
        var jsonContent = JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, jsonContent);

        return filePath;
    }

    public async Task<Dictionary<string, string>> LoadExpectedContentAsync()
    {
        return new Dictionary<string, string>
        {
            { "company_name", "Test Company Ltd" },
            { "project_title", "E2E Integration Test Project" }, 
            { "current_date", "2025-08-18" },
            { "author_name", "E2E Test Suite" }
        };
    }

    public void CleanupTestData(string directory)
    {
        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}