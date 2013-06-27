// Copyright (c) Yuriy Guts, 2013
// 
// Licensed under the Apache License, version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at:
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
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
            Trace.TraceInformation("Recognizing image: {0} --> {1}", inputFileName, outputFileName);

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
