namespace DocxTemplate.UI.Models;

/// <summary>
/// Defines the processing mode for the wizard workflow
/// </summary>
public enum ProcessingMode
{
    /// <summary>
    /// Nova zakazka - New Project workflow for processing fresh templates
    /// </summary>
    NewProject,
    
    /// <summary>
    /// Uprava zakazky - Project Update workflow for completing partially filled templates
    /// </summary>
    UpdateProject
}