using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Windows.Forms;
using System.Collections;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using Ghostscript.NET.Processor;

namespace PdfSplitCsharp
{
    class Program
    {

        static void Main(string[] args)
        {
            
            GhostscriptRasterizer r = new GhostscriptRasterizer();

            String inputFile = "c:\\users\\masons\\projects\\pdfsplitcsharp\\test.pdf";
            String outputDir = "c:\\users\\masons\\projects\\pdfsplitcsharp\\";

            r.Open(inputFile);
            
            Bitmap image;
            String json = "";

            int fc = 0;
            int c = 0;
            int f = 0;
            String doctype = "transcript";


            IDictionary<int, String> docNames = new Dictionary<int, String>();
            IDictionary<int, int> startPage = new Dictionary<int, int>();            
            IDictionary<int, int> endPage = new Dictionary<int, int>();

            for (int i = 1; i <= r.PageCount; i++)
            {

                image = (Bitmap)r.GetPage(72, 72, i);
                json = Program.GetQr(image);

                if (!json.Equals("nothing"))
                {
                    fc++; // increment document counter
                    doctype = "transcript"; // we'll set this based on the JSON

                    // create new file
                    docNames[fc] = doctype + "_" + fc.ToString() + ".pdf";

                }
                else
                {
                    if (startPage.ContainsKey(fc))
                    {
                        endPage[fc] = i;
                    }
                    else
                    {
                        startPage[fc] = i;
                    }
                    
                }

            }
            
            r.Close();
            r.Dispose();

            Boolean pdfBuilt;

            for (int i = 1; i <= fc; i++)
            {
                pdfBuilt = BuildPdf(startPage[i], endPage[i], inputFile, outputDir + docNames[i]);
            }

            

        }

        static Boolean BuildPdf(int FirstPage, int LastPage, String inputFile, String outputFile)
        {

            Boolean result = true;

            try
            {
                GhostscriptProcessor processor = new GhostscriptProcessor();

                List<string> gsArgs = new List<string>();

                gsArgs.Add("-empty");
                gsArgs.Add("-dBATCH");
                gsArgs.Add(String.Format(@"-sOutputFile={0}", outputFile)); // output file
                gsArgs.Add(String.Format(@"-dFirstPage={0}", FirstPage.ToString()));
                gsArgs.Add(String.Format(@"-dLastPage={0}", LastPage.ToString()));
                gsArgs.Add("-sDEVICE=pdfwrite");
                gsArgs.Add(inputFile); // input file

                string[] gsArgsArray = gsArgs.ToArray();

                processor.Process(gsArgsArray);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        static String GetQr(Bitmap image)
        {

            String json = "";

            using (image)
            {
                LuminanceSource source;
                source = new BitmapLuminanceSource(image);
                BinaryBitmap bitmap = new BinaryBitmap(new GlobalHistogramBinarizer(source));

                Dictionary<DecodeHintType, object> hints = new Dictionary<DecodeHintType, object>();
                ArrayList fmts = new ArrayList();
                
                fmts.Add(BarcodeFormat.QR_CODE);

                hints.Add(DecodeHintType.TRY_HARDER, true);
                hints.Add(DecodeHintType.POSSIBLE_FORMATS, fmts);

                Reader reader = new QRCodeReader();

                Result result = (Result)reader.decode(bitmap, hints);

                if (result != null)
                {
                    json = result.Text;
                }
                else
                {
                    json = "nothing";
                }
            }

            return json;
        }

    }
}


