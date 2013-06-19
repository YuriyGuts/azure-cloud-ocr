using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Tesseract;

namespace TesseractProcessor
{
    class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Invalid number of arguments!");
                Console.WriteLine("Usage: \"TesseractProcessor <InputImageFile> <OutputTextFile>\"");
                return 1;
            }

            var inputFileName = args[0];
            var outputFileName = args[1];

            var inputImage = Image.FromFile(inputFileName);
            var recognizedText = RecognizeTextFromImage(inputImage);
            File.WriteAllText(outputFileName, recognizedText, new UTF8Encoding(false));
            
            return 0;
        }

        private static string RecognizeTextFromImage(Image image)
        {
            string recognizedText;

            using (var ocrEngine = new TesseractEngine(@"./tessdata", "eng"))
            using (Bitmap bitmap = new Bitmap(image))
            using (Bitmap monochromeBitmap = ImageUtils.Convert24BitToMonochrome(bitmap))
            using (var page = ocrEngine.Process(monochromeBitmap))
            {
                recognizedText = page.GetText();
                Trace.TraceInformation("Text recognized with mean confidence: {0:N3}", page.GetMeanConfidence());
            }

            return recognizedText;
        }
    }
}
