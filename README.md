# PDF Splitter
This C# program uses Ghostcript to read PDFs into images so that ZXing can see if any of the pages have QR codes that have a "document-type" key as decoded by NewtonSoft's Json package. If it finds a QR code on a page, that page is considered a separator page marking the start of a document of that indicated type. It then builds separate documents using the iTextSharp library.

# Running it
This is meant for Windows 10. There are no un-included dependencies. Build this for x64 using Visual Studio and deploy using the files in the "publish/Application Files/pdfsplit_version_blah/" folder. Make sure that the 64-bit DLL for Ghostscript is included.
