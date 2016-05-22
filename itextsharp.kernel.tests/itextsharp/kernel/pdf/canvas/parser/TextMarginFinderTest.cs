using System;
using NUnit.Framework;
using iTextSharp.Kernel.Geom;
using iTextSharp.Kernel.Pdf;
using iTextSharp.Kernel.Pdf.Canvas.Parser.Listener;
using iTextSharp.Test;

namespace iTextSharp.Kernel.Pdf.Canvas.Parser
{
	public class TextMarginFinderTest : ExtendedITextTest
	{
		private const String sourceFolder = "../../resources/itextsharp/kernel/parser/TextMarginFinderTest/";

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void Test()
		{
			TextMarginFinder finder = new TextMarginFinder();
			PdfDocument pdfDocument = new PdfDocument(new PdfReader(sourceFolder + "in.pdf"));
			new PdfCanvasProcessor(finder).ProcessPageContent(pdfDocument.GetPage(1));
			Rectangle textRect = finder.GetTextRectangle();
			NUnit.Framework.Assert.AreEqual(1.42f * 72f, textRect.GetX(), 0.01f);
			NUnit.Framework.Assert.AreEqual(7.42f * 72f, textRect.GetX() + textRect.GetWidth(
				), 0.01f);
			NUnit.Framework.Assert.AreEqual(2.42f * 72f, textRect.GetY(), 0.01f);
			NUnit.Framework.Assert.AreEqual(10.42f * 72f, textRect.GetY() + textRect.GetHeight
				(), 0.01f);
		}
	}
}