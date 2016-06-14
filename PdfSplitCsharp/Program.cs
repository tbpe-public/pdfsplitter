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
using Newtonsoft.Json;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace PdfSplitCsharp
{
    class Program
    {

        static void Main(string[] args)
        {

            // let's get a DLL that we want to deploy
            GhostscriptVersionInfo gvi = new GhostscriptVersionInfo(AppDomain.CurrentDomain.BaseDirectory + @"\gsdll64.dll");

            // this is what will read the PDF file and turn it into an image
            GhostscriptRasterizer r = new GhostscriptRasterizer();

            String inputFile = @"";
            String outputDir = @"";
            String singleFileName = @"";

            String workingDirectory = Directory.GetCurrentDirectory();

            string[] files = Directory.GetFiles(workingDirectory, "*.pdf");

            foreach (string filename in files)
            {
                singleFileName = Path.GetFileNameWithoutExtension(filename);
                DialogResult dialogresult = MessageBox.Show("Found a file to process: " + singleFileName + ".pdf" + "\nContinue?", "Program Step 1", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (dialogresult == DialogResult.Cancel)
                {
                    return;
                }
                else
                {
                    inputFile = filename;
                    break;
                }
            }


            if (args.Length > 0)
            {
                inputFile = args[0];
            }

            if (inputFile.Length < 5)
            {
                Console.WriteLine("Please provide a PDF file to process.");
                return;
            }

            try
            {
                r.Open(inputFile, gvi, false); // use the DLL we referenced at the start of this program
            }
            catch (Exception x)
            {

                Console.WriteLine("Could not start up Ghostscript Rasterizer: " + x.ToString());
                return;
            }

            
            Bitmap image;
            String json = "";

            int fc = 0;
            int pagecount = 0;
            int totalpagecount = 0;
            int dividerpagecount = 0;
            int pdfpagecount = r.PageCount;
            String doctype = "misc";

            // store output document names, and the pages of which these are to be comprised
            IDictionary<int, String> docNames = new Dictionary<int, String>();
            IDictionary<int, int> startPage = new Dictionary<int, int>();            
            IDictionary<int, int> endPage = new Dictionary<int, int>();
            IDictionary<int, int> countPages = new Dictionary<int, int>();

            QrResult qrs;

            // loop over all the pages, looking for QR codes to indicate the document start
            for (int i = 1; i <= r.PageCount; i++)
            {

                image = (Bitmap)r.GetPage(72, 72, i);
                json = Program.GetQr(image);

                // this object may already be set from previous, so clear it
                qrs = null;

                // if we get some JSON, deserialize it
                if (json.Length > 0)
                {

                    try
                    {
                        qrs = JsonConvert.DeserializeObject<QrResult>(json);
                    }
                    catch
                    {
                        // well let's just ignore this
                    }
                }


                // if we found a QR code with "document-type", we know this begins a new document
                if (qrs != null && qrs.documenttype.Length > 0)
                {

                    fc++; // new document!
                    doctype = qrs.documenttype; // we'll set this based on the JSON valye of the "document-type" key

                    // create new file
                    docNames[fc] = doctype + "_" + fc.ToString();
                    pagecount = 0; // reset our per-file page counter
                    dividerpagecount++; // keep track of how many separator pages we've got

                }
                else
                {
                    // we didn't find a QR code with "document-type" as a key on this page

                    // if we already have a starting page for this document, then let's update the last page (it'll keep going up until we find a new document)
                    if (startPage.ContainsKey(fc))
                    {
                        endPage[fc] = i;
                    }
                    else // if we don't have a starting page for this document, set it
                    {
                        startPage[fc] = i;
                    }

                    
                    totalpagecount++; // increment our total number of content pages

                    pagecount++;
                    countPages[fc] = pagecount; // store our per-file page counter
                    
                }

            }
            
            // empty our Rasterizer objects
            r.Close();
            r.Dispose();

            Boolean pdfBuilt;

            // loop over all the files we need to create, building a PDF for each
            for (int i = 1; i <= fc; i++)
            {
                pdfBuilt = BuildPdf(startPage[i], endPage[i], inputFile, outputDir + singleFileName + "_" + docNames[i] + "_" + countPages[i].ToString() + ".pdf");
            }

            MessageBox.Show("Total pages overall: " + pdfpagecount.ToString() + "\n" + "Total individual documents: " + dividerpagecount.ToString() + "\n" + "Total content pages: " + totalpagecount.ToString(), "All Done");

            

        }

        static Boolean BuildPdf(int FirstPage, int LastPage, String inputFile, String outputFile)
        {

            Boolean result = true;

            PdfReader reader = null;
            Document document = null;
            PdfCopy pdfCopyProvider = null;
            PdfImportedPage importedPage = null;

            // Intialize a new PdfReader instance with the contents of the source Pdf file:
            reader = new PdfReader(inputFile);

            // For simplicity, I am assuming all the pages share the same size
            // and rotation as the first page:
            document = new Document(reader.GetPageSizeWithRotation(FirstPage));

            // Initialize an instance of the PdfCopyClass with the source 
            // document and an output file stream:
            pdfCopyProvider = new PdfCopy(document,
                new System.IO.FileStream(outputFile, System.IO.FileMode.Create));

            document.Open();

            // Walk the specified range and add the page copies to the output file:
            for (int i = FirstPage; i <= LastPage; i++)
            {
                importedPage = pdfCopyProvider.GetImportedPage(reader, i);
                pdfCopyProvider.AddPage(importedPage);
            }
            document.Close();
            reader.Close();

            return result;

        }

        /**
         * Use the Ghostscript library to build a PDF file based on an input PDF and the pages you want to use
         * returns False if fails, true if success
         * 
         */
        static Boolean BuildPdfGs(int FirstPage, int LastPage, String inputFile, String outputFile)
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

                gsArgs.Add("-dAutoFilterColorImages=false");
                gsArgs.Add("-dAutoFilterGrayImages=false");
                gsArgs.Add("-dColorImageFilter=/FlateEncode");
                gsArgs.Add("-dGrayImageFilter=/FlateEncode");

                gsArgs.Add("-dColorConversionStrategy=/LeaveColorUnchanged");
                gsArgs.Add("-dDownsampleMonoImages=false");
                gsArgs.Add("-dDownsampleGrayImages=false");
                gsArgs.Add("-dDownsampleColorImages=false");

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

        // read a QR code from an image and return the plaintext, otherwise return "nothing"
        static String GetQr(Bitmap image)
        {

            String json = "";

            using (image)
            {
                // use some zxing magic to get our image ready to read as a bitmap
                LuminanceSource source;
                source = new BitmapLuminanceSource(image);
                BinaryBitmap bitmap = new BinaryBitmap(new GlobalHistogramBinarizer(source)); // GlobalHistogramBinarizer is the only binarizer I could get to work

                // create some hints to help the QRCodeReader
                Dictionary<DecodeHintType, object> hints = new Dictionary<DecodeHintType, object>();
                ArrayList fmts = new ArrayList();
                fmts.Add(BarcodeFormat.QR_CODE);
                hints.Add(DecodeHintType.TRY_HARDER, true);
                hints.Add(DecodeHintType.POSSIBLE_FORMATS, fmts);

                // get a QRCodeReader and decode it, passing in the binary bitmap and hints
                Reader reader = new QRCodeReader();
                Result result = (Result)reader.decode(bitmap, hints);

                // if we found a result, return the plaintext (should be JSON); if we didn't return "nothing"
                if (result != null)
                {
                    //json = @"{'result':'success','document-type':'transcript'}";
                    
                    json = result.Text.Replace("\\", ""); // there's some weird escaping going on
                }
                else
                {
                    //json = @"{'result':'failure','document-type':'nothing'}";
                    json = @"";
                }
            }

            return json;
        }

    }

}


