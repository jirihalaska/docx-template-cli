using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace DocxTemplate.Research
{
    /// <summary>
    /// POC for inserting images into DOCX files with size control
    /// </summary>
    public class ImageInsertionPoc
    {
        // Constants for EMU conversion
        private const int EmusPerInch = 914400;
        private const int EmusPerCm = 360000;
        private const int EmusPerPixel = 9525;

        /// <summary>
        /// Option 1: Replace a text placeholder with an image
        /// </summary>
        public static void ReplaceTextPlaceholderWithImage(string docPath, string placeholderText, string imagePath, int maxWidthPixels, int maxHeightPixels)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(docPath, true))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                
                // Find text elements containing the placeholder
                var textElements = body.Descendants<Text>()
                    .Where(t => t.Text.Contains(placeholderText))
                    .ToList();

                foreach (var textElement in textElements)
                {
                    // Add the image part
                    MainDocumentPart mainPart = wordDoc.MainDocumentPart;
                    ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
                    
                    using (FileStream stream = new FileStream(imagePath, FileMode.Open))
                    {
                        imagePart.FeedData(stream);
                    }
                    
                    // Get the image dimensions with aspect ratio preserved
                    var (widthEmus, heightEmus) = CalculateImageDimensions(imagePath, maxWidthPixels, maxHeightPixels);
                    
                    // Create the Drawing element
                    var element = CreateImageElement(mainPart.GetIdOfPart(imagePart), widthEmus, heightEmus);
                    
                    // Replace the text with the image
                    var paragraph = textElement.Parent.Parent as Paragraph;
                    if (paragraph != null)
                    {
                        // Clear the paragraph and add the image
                        paragraph.RemoveAllChildren<Run>();
                        paragraph.AppendChild(new Run(element));
                    }
                }
                
                wordDoc.MainDocumentPart.Document.Save();
            }
        }

        /// <summary>
        /// Option 2: Use Picture Content Controls as placeholders
        /// </summary>
        public static void ReplacePictureContentControl(string docPath, string tagName, string imagePath, bool preserveAspectRatio = true)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(docPath, true))
            {
                // Find the content control by tag
                var sdtElements = wordDoc.MainDocumentPart.Document.Descendants<SdtElement>()
                    .Where(sdt => sdt.SdtProperties.GetFirstChild<Tag>()?.Val == tagName)
                    .ToList();

                foreach (var sdt in sdtElements)
                {
                    // Check if it's a picture content control
                    if (sdt.SdtProperties.GetFirstChild<SdtContentPicture>() != null)
                    {
                        // Get existing drawing element and its dimensions
                        var existingDrawing = sdt.Descendants<Drawing>().FirstOrDefault();
                        if (existingDrawing != null)
                        {
                            var inline = existingDrawing.GetFirstChild<Inline>();
                            if (inline != null)
                            {
                                // Get the placeholder dimensions
                                long maxWidth = inline.Extent.Cx;
                                long maxHeight = inline.Extent.Cy;
                                
                                // Replace the image
                                var blip = existingDrawing.Descendants<A.Blip>().FirstOrDefault();
                                if (blip != null)
                                {
                                    MainDocumentPart mainPart = wordDoc.MainDocumentPart;
                                    
                                    // Remove old image part
                                    if (mainPart.GetPartById(blip.Embed) is ImagePart oldImagePart)
                                    {
                                        mainPart.DeletePart(oldImagePart);
                                    }
                                    
                                    // Add new image part
                                    ImagePart newImagePart = mainPart.AddImagePart(GetImagePartType(imagePath));
                                    using (FileStream stream = new FileStream(imagePath, FileMode.Open))
                                    {
                                        newImagePart.FeedData(stream);
                                    }
                                    
                                    // Update the reference
                                    blip.Embed = mainPart.GetIdOfPart(newImagePart);
                                    
                                    // Update dimensions if preserving aspect ratio
                                    if (preserveAspectRatio)
                                    {
                                        var (newWidth, newHeight) = CalculateImageDimensionsFromEmus(imagePath, maxWidth, maxHeight);
                                        inline.Extent.Cx = newWidth;
                                        inline.Extent.Cy = newHeight;
                                        
                                        // Also update the picture shape properties
                                        var shapeProperties = existingDrawing.Descendants<PIC.ShapeProperties>().FirstOrDefault();
                                        if (shapeProperties?.Transform2D?.Extents != null)
                                        {
                                            shapeProperties.Transform2D.Extents.Cx = newWidth;
                                            shapeProperties.Transform2D.Extents.Cy = newHeight;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                wordDoc.MainDocumentPart.Document.Save();
            }
        }

        /// <summary>
        /// Option 3: Use a shape/textbox as a container for the image
        /// </summary>
        public static void InsertImageInShapeContainer(string docPath, string shapeName, string imagePath)
        {
            // This would involve finding shapes by name and inserting images within them
            // Shapes can have fixed dimensions that act as boundaries for the image
            throw new NotImplementedException("Shape container approach needs further research");
        }

        /// <summary>
        /// Calculate image dimensions preserving aspect ratio
        /// </summary>
        private static (long widthEmus, long heightEmus) CalculateImageDimensions(string imagePath, int maxWidthPixels, int maxHeightPixels)
        {
            // In a real implementation, use an image library to get actual dimensions
            // For POC, we'll use placeholder logic
            System.Drawing.Image img = System.Drawing.Image.FromFile(imagePath);
            int originalWidth = img.Width;
            int originalHeight = img.Height;
            img.Dispose();
            
            // Calculate aspect ratio
            double aspectRatio = (double)originalHeight / originalWidth;
            
            // Calculate scaled dimensions
            int finalWidth = maxWidthPixels;
            int finalHeight = (int)(maxWidthPixels * aspectRatio);
            
            // Check if height exceeds max
            if (finalHeight > maxHeightPixels)
            {
                finalHeight = maxHeightPixels;
                finalWidth = (int)(maxHeightPixels / aspectRatio);
            }
            
            // Convert to EMUs
            long widthEmus = finalWidth * EmusPerPixel;
            long heightEmus = finalHeight * EmusPerPixel;
            
            return (widthEmus, heightEmus);
        }

        /// <summary>
        /// Calculate image dimensions from EMU values preserving aspect ratio
        /// </summary>
        private static (long widthEmus, long heightEmus) CalculateImageDimensionsFromEmus(string imagePath, long maxWidthEmus, long maxHeightEmus)
        {
            // Get original image dimensions
            System.Drawing.Image img = System.Drawing.Image.FromFile(imagePath);
            int originalWidth = img.Width;
            int originalHeight = img.Height;
            img.Dispose();
            
            // Calculate aspect ratio
            double aspectRatio = (double)originalHeight / originalWidth;
            
            // Calculate scaled dimensions in EMUs
            long finalWidth = maxWidthEmus;
            long finalHeight = (long)(maxWidthEmus * aspectRatio);
            
            // Check if height exceeds max
            if (finalHeight > maxHeightEmus)
            {
                finalHeight = maxHeightEmus;
                finalWidth = (long)(maxHeightEmus / aspectRatio);
            }
            
            return (finalWidth, finalHeight);
        }

        /// <summary>
        /// Create the Drawing element for an image
        /// </summary>
        private static Drawing CreateImageElement(string relationshipId, long widthEmus, long heightEmus)
        {
            // Create unique IDs for this image
            var docPropId = (uint)new Random().Next(1, 100000);
            var docPropName = "Picture " + docPropId;
            
            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                    new DW.EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DW.DocProperties()
                    {
                        Id = docPropId,
                        Name = docPropName
                    },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks() { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties()
                                    {
                                        Id = docPropId,
                                        Name = docPropName
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip(
                                        new A.BlipExtensionList(
                                            new A.BlipExtension()
                                            {
                                                Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                            })
                                    )
                                    {
                                        Embed = relationshipId,
                                        CompressionState = A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(
                                        new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                    new A.PresetGeometry(
                                        new A.AdjustValueList()
                                    ) { Preset = A.ShapeTypeValues.Rectangle }))
                        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                {
                    DistanceFromTop = (UInt32Value)0U,
                    DistanceFromBottom = (UInt32Value)0U,
                    DistanceFromLeft = (UInt32Value)0U,
                    DistanceFromRight = (UInt32Value)0U
                });

            return element;
        }

        /// <summary>
        /// Get the appropriate ImagePartType based on file extension
        /// </summary>
        private static ImagePartType GetImagePartType(string imagePath)
        {
            string extension = System.IO.Path.GetExtension(imagePath).ToLower();
            return extension switch
            {
                ".png" => ImagePartType.Png,
                ".gif" => ImagePartType.Gif,
                ".bmp" => ImagePartType.Bmp,
                ".tiff" or ".tif" => ImagePartType.Tiff,
                _ => ImagePartType.Jpeg, // Default to JPEG
            };
        }
    }
}