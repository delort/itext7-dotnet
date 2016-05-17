/*
$Id$

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using System.IO;
using Java.IO;
using iTextSharp.IO;
using iTextSharp.IO.Log;
using iTextSharp.IO.Source;
using iTextSharp.Kernel;
using iTextSharp.Kernel.Pdf.Filters;

namespace iTextSharp.Kernel.Pdf
{
	public class PdfReader
	{
		private const long serialVersionUID = -3584187443691964939L;

		private const String endstream1 = "endstream";

		private const String endstream2 = "\nendstream";

		private const String endstream3 = "\r\nendstream";

		private const String endstream4 = "\rendstream";

		private static readonly byte[] endstream = ByteUtils.GetIsoBytes("endstream");

		private static readonly byte[] endobj = ByteUtils.GetIsoBytes("endobj");

		protected internal static bool correctStreamLength = true;

		private bool unethicalReading;

		private PdfIndirectReference currentIndirectReference;

		private String sourcePath;

		protected internal PdfTokenizer tokens;

		protected internal PdfEncryption decrypt;

		protected internal PdfVersion headerPdfVersion;

		protected internal long lastXref;

		protected internal long eofPos;

		protected internal PdfDictionary trailer;

		protected internal PdfDocument pdfDocument;

		protected internal PdfAConformanceLevel pdfAConformanceLevel;

		protected internal ReaderProperties properties;

		protected internal bool encrypted = false;

		protected internal bool rebuiltXref = false;

		protected internal bool hybridXref = false;

		protected internal bool fixedXref = false;

		protected internal bool xrefStm = false;

		/// <summary>Constructs a new PdfReader.</summary>
		/// <param name="byteSource">source of bytes for the reader</param>
		/// <param name="properties">properties of the created reader</param>
		/// <exception cref="System.IO.IOException"/>
		public PdfReader(IRandomAccessSource byteSource, ReaderProperties properties)
		{
			//indicate nearest first Indirect reference object which includes current reading the object, using for PdfString decrypt
			// For internal usage only
			// here we store only the pdfVersion that is written in the document's header,
			// however it could differ from the actual pdf version that could be written in document's catalog
			this.properties = properties;
			this.tokens = GetOffsetTokeniser(byteSource);
		}

		/// <summary>Reads and parses a PDF document.</summary>
		/// <param name="is">
		/// the
		/// <c>InputStream</c>
		/// containing the document. The stream is read to the
		/// end but is not closed
		/// </param>
		/// <param name="properties">properties of the created reader</param>
		/// <exception cref="System.IO.IOException">on error</exception>
		/// <exception cref="iTextSharp.Kernel.PdfException">on error</exception>
		public PdfReader(Stream @is, ReaderProperties properties)
			: this(new RandomAccessSourceFactory().CreateSource(@is), properties)
		{
		}

		/// <summary>Reads and parses a PDF document.</summary>
		/// <param name="is">
		/// the
		/// <c>InputStream</c>
		/// containing the document. Stream is closed automatically, when document is closed,
		/// if user doesn't want to close stream, he should set closeStream=false;
		/// </param>
		/// <exception cref="System.IO.IOException">on error</exception>
		/// <exception cref="iTextSharp.Kernel.PdfException">on error</exception>
		public PdfReader(Stream @is)
			: this(@is, new ReaderProperties())
		{
		}

		/// <summary>Reads and parses a PDF document.</summary>
		/// <param name="filename">the file name of the document</param>
		/// <param name="properties">properties of the created reader</param>
		/// <exception cref="System.IO.IOException">on error</exception>
		public PdfReader(String filename, ReaderProperties properties)
			: this(new RandomAccessSourceFactory().SetForceRead(false).CreateBestSource(filename
				), properties)
		{
			this.sourcePath = filename;
		}

		/// <summary>Reads and parses a PDF document.</summary>
		/// <param name="filename">the file name of the document</param>
		/// <exception cref="System.IO.IOException">on error</exception>
		public PdfReader(String filename)
			: this(filename, new ReaderProperties())
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Close()
		{
			tokens.Close();
		}

		public virtual iTextSharp.Kernel.Pdf.PdfReader SetUnethicalReading(bool unethicalReading
			)
		{
			this.unethicalReading = unethicalReading;
			return this;
		}

		public virtual bool IsCloseStream()
		{
			return tokens.IsCloseStream();
		}

		public virtual void SetCloseStream(bool closeStream)
		{
			tokens.SetCloseStream(closeStream);
		}

		/// <summary>If any exception generated while reading XRef section, PdfReader will try to rebuild it.
		/// 	</summary>
		/// <returns>true, if PdfReader rebuilt Cross-Reference section.</returns>
		public virtual bool HasRebuiltXref()
		{
			return rebuiltXref;
		}

		/// <summary>
		/// Some documents contain hybrid XRef, for more information see "7.5.8.4 Compatibility with Applications
		/// That Do Not Support Compressed Reference Streams" in PDF 32000-1:2008 spec.
		/// </summary>
		/// <returns>true, if the document has hybrid Cross-Reference section.</returns>
		public virtual bool HasHybridXref()
		{
			return hybridXref;
		}

		/// <summary>Indicates whether the document has Cross-Reference Streams.</summary>
		/// <returns>true, if the document has Cross-Reference Streams.</returns>
		public virtual bool HasXrefStm()
		{
			return xrefStm;
		}

		/// <summary>If any exception generated while reading PdfObject, PdfReader will try to fix offsets of all objects.
		/// 	</summary>
		/// <returns>true, if PdfReader fixed offsets of PdfObjects.</returns>
		public virtual bool HasFixedXref()
		{
			return fixedXref;
		}

		/// <summary>Gets position of the last Cross-Reference table.</summary>
		/// <returns>-1 if Cross-Reference table has rebuilt, otherwise position of the last Cross-Reference table.
		/// 	</returns>
		public virtual long GetLastXref()
		{
			return lastXref;
		}

		/// <summary>Reads and gets stream bytes.</summary>
		/// <param name="decode">true if to get decoded stream bytes, false if to leave it originally encoded.
		/// 	</param>
		/// <returns>byte[]</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="iTextSharp.Kernel.PdfException"/>
		public virtual byte[] ReadStreamBytes(PdfStream stream, bool decode)
		{
			byte[] b = ReadStreamBytesRaw(stream);
			if (decode && b != null)
			{
				return DecodeBytes(b, stream);
			}
			else
			{
				return b;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual byte[] ReadStreamBytesRaw(PdfStream stream)
		{
			PdfName type = stream.GetAsName(PdfName.Type);
			if (!PdfName.XRefStm.Equals(type) && !PdfName.ObjStm.Equals(type))
			{
				CheckPdfStreamLength(stream);
			}
			long offset = stream.GetOffset();
			if (offset <= 0)
			{
				return null;
			}
			int length = stream.GetLength();
			if (length <= 0)
			{
				return new byte[0];
			}
			RandomAccessFileOrArray file = tokens.GetSafeFile();
			byte[] bytes = null;
			try
			{
				file.Seek(stream.GetOffset());
				bytes = new byte[length];
				file.ReadFully(bytes);
				if (decrypt != null)
				{
					PdfObject filter = stream.Get(PdfName.Filter, true);
					bool skip = false;
					if (filter != null)
					{
						if (PdfName.Crypt.Equals(filter))
						{
							skip = true;
						}
						else
						{
							if (filter.GetType() == PdfObject.ARRAY)
							{
								PdfArray filters = (PdfArray)filter;
								for (int k = 0; k < filters.Size(); k++)
								{
									if (!filters.IsEmpty() && PdfName.Crypt.Equals(filters.Get(k, true)))
									{
										skip = true;
										break;
									}
								}
							}
						}
						filter.Release();
					}
					if (!skip)
					{
						decrypt.SetHashKeyForNextObject(stream.GetIndirectReference().GetObjNumber(), stream
							.GetIndirectReference().GetGenNumber());
						bytes = decrypt.DecryptByteArray(bytes);
					}
				}
			}
			finally
			{
				try
				{
					file.Close();
				}
				catch (Exception)
				{
				}
			}
			return bytes;
		}

		/// <summary>Gets the input stream associated with PdfStream.</summary>
		/// <remarks>
		/// Gets the input stream associated with PdfStream.
		/// User is responsible for closing returned stream.
		/// </remarks>
		/// <param name="decode">true if to get decoded stream, false if to leave it originally encoded.
		/// 	</param>
		/// <returns>InputStream</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="iTextSharp.Kernel.PdfException"/>
		public virtual Stream ReadStream(PdfStream stream, bool decode)
		{
			byte[] bytes = ReadStreamBytes(stream, decode);
			return bytes != null ? new MemoryStream(bytes) : null;
		}

		/// <summary>Decode a byte[] applying the filters specified in the provided dictionary using default filter handlers.
		/// 	</summary>
		/// <param name="b">the bytes to decode</param>
		/// <param name="streamDictionary">the dictionary that contains filter information</param>
		/// <returns>the decoded bytes</returns>
		/// <exception cref="iTextSharp.Kernel.PdfException">if there are any problems decoding the bytes
		/// 	</exception>
		public static byte[] DecodeBytes(byte[] b, PdfDictionary streamDictionary)
		{
			return DecodeBytes(b, streamDictionary, FilterHandlers.GetDefaultFilterHandlers()
				);
		}

		/// <summary>Decode a byte[] applying the filters specified in the provided dictionary using the provided filter handlers.
		/// 	</summary>
		/// <param name="b">the bytes to decode</param>
		/// <param name="streamDictionary">the dictionary that contains filter information</param>
		/// <param name="filterHandlers">the map used to look up a handler for each type of filter
		/// 	</param>
		/// <returns>the decoded bytes</returns>
		/// <exception cref="iTextSharp.Kernel.PdfException">if there are any problems decoding the bytes
		/// 	</exception>
		public static byte[] DecodeBytes(byte[] b, PdfDictionary streamDictionary, IDictionary
			<PdfName, IFilterHandler> filterHandlers)
		{
			if (b == null)
			{
				return null;
			}
			PdfObject filter = streamDictionary.Get(PdfName.Filter);
			PdfArray filters = new PdfArray();
			if (filter != null)
			{
				if (filter.GetType() == PdfObject.NAME)
				{
					filters.Add(filter);
				}
				else
				{
					if (filter.GetType() == PdfObject.ARRAY)
					{
						filters = ((PdfArray)filter);
					}
				}
			}
			PdfArray dp = new PdfArray();
			PdfObject dpo = streamDictionary.Get(PdfName.DecodeParms);
			if (dpo == null || (dpo.GetType() != PdfObject.DICTIONARY && dpo.GetType() != PdfObject
				.ARRAY))
			{
				if (dpo != null)
				{
					dpo.Release();
				}
				dpo = streamDictionary.Get(PdfName.DP);
			}
			if (dpo != null)
			{
				if (dpo.GetType() == PdfObject.DICTIONARY)
				{
					dp.Add(dpo);
				}
				else
				{
					if (dpo.GetType() == PdfObject.ARRAY)
					{
						dp = ((PdfArray)dpo);
					}
				}
				dpo.Release();
			}
			for (int j = 0; j < filters.Size(); ++j)
			{
				PdfName filterName = (PdfName)filters.Get(j);
				IFilterHandler filterHandler = filterHandlers[filterName];
				if (filterHandler == null)
				{
					throw new PdfException(PdfException.Filter1IsNotSupported).SetMessageParams(filterName
						);
				}
				PdfDictionary decodeParams;
				if (j < dp.Size())
				{
					PdfObject dpEntry = dp.Get(j, true);
					if (dpEntry == null || dpEntry.GetType() == PdfObject.NULL)
					{
						decodeParams = null;
					}
					else
					{
						if (dpEntry.GetType() == PdfObject.DICTIONARY)
						{
							decodeParams = (PdfDictionary)dpEntry;
						}
						else
						{
							throw new PdfException(PdfException.DecodeParameterType1IsNotSupported).SetMessageParams
								(dpEntry.GetType().ToString());
						}
					}
				}
				else
				{
					decodeParams = null;
				}
				b = filterHandler.Decode(b, filterName, decodeParams, streamDictionary);
			}
			return b;
		}

		/// <summary>
		/// Gets a new file instance of the original PDF
		/// document.
		/// </summary>
		/// <returns>a new file instance of the original PDF document</returns>
		public virtual RandomAccessFileOrArray GetSafeFile()
		{
			return tokens.GetSafeFile();
		}

		/// <summary>Provides the size of the opened file.</summary>
		/// <returns>The size of the opened file.</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual long GetFileLength()
		{
			return tokens.GetSafeFile().Length();
		}

		public virtual bool IsOpenedWithFullPermission()
		{
			return !encrypted || decrypt.IsOpenedWithFullPermission() || unethicalReading;
		}

		public virtual long GetPermissions()
		{
			long perm = 0;
			if (encrypted && decrypt.GetPermissions() != null)
			{
				perm = decrypt.GetPermissions();
			}
			return perm;
		}

		public virtual int GetCryptoMode()
		{
			if (decrypt == null)
			{
				return -1;
			}
			else
			{
				return decrypt.GetCryptoMode();
			}
		}

		/// <summary>Gets the declared Pdf/A conformance level of the source document that is being read.
		/// 	</summary>
		/// <remarks>
		/// Gets the declared Pdf/A conformance level of the source document that is being read.
		/// Note that this information is provided via XMP metadata and is not verified by iText.
		/// </remarks>
		/// <returns>
		/// conformance level of the source document, or
		/// <see langword="null"/>
		/// if no Pdf/A
		/// conformance level information is specified.
		/// </returns>
		public virtual PdfAConformanceLevel GetPdfAConformanceLevel()
		{
			return pdfAConformanceLevel;
		}

		/// <summary>Computes user password if standard encryption handler is used with Standard40, Standard128 or AES128 encryption algorithm.
		/// 	</summary>
		/// <returns>user password, or null if not a standard encryption handler was used or if ownerPasswordUsed wasn't use to open the document.
		/// 	</returns>
		public virtual byte[] ComputeUserPassword()
		{
			if (!encrypted || !decrypt.IsOpenedWithFullPermission())
			{
				return null;
			}
			return decrypt.ComputeUserPassword(properties.password);
		}

		/// <summary>Parses the entire PDF</summary>
		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void ReadPdf()
		{
			String version = tokens.CheckPdfHeader();
			try
			{
				this.headerPdfVersion = PdfVersion.FromString(version);
			}
			catch (ArgumentException)
			{
				throw new PdfException(PdfException.PdfVersionNotValid, version);
			}
			try
			{
				ReadXref();
			}
			catch (Exception ex)
			{
				ILogger logger = LoggerFactory.GetLogger(typeof(iTextSharp.Kernel.Pdf.PdfReader));
				logger.Error(LogMessageConstant.XREF_ERROR, ex);
				RebuildXref();
			}
			ReadDecryptObj();
		}

		private void ReadDecryptObj()
		{
			if (encrypted)
			{
				return;
			}
			PdfDictionary enc = trailer.GetAsDictionary(PdfName.Encrypt);
			if (enc == null)
			{
				return;
			}
			encrypted = true;
			PdfName filter = enc.GetAsName(PdfName.Filter);
			if (PdfName.Adobe_PubSec.Equals(filter))
			{
				decrypt = new PdfEncryption(enc, properties.certificateKey, properties.certificate
					, properties.certificateKeyProvider, properties.externalDecryptionProcess);
			}
			else
			{
				if (PdfName.Standard.Equals(filter))
				{
					decrypt = new PdfEncryption(enc, properties.password, GetOriginalFileId());
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void ReadObjectStream(PdfStream objectStream)
		{
			int objectStreamNumber = objectStream.GetIndirectReference().GetObjNumber();
			int first = objectStream.GetAsNumber(PdfName.First).IntValue();
			int n = objectStream.GetAsNumber(PdfName.N).IntValue();
			byte[] bytes = ReadStreamBytes(objectStream, true);
			PdfTokenizer saveTokens = tokens;
			try
			{
				tokens = new PdfTokenizer(new RandomAccessFileOrArray(new RandomAccessSourceFactory
					().CreateSource(bytes)));
				int[] address = new int[n];
				int[] objNumber = new int[n];
				bool ok = true;
				for (int k = 0; k < n; ++k)
				{
					ok = tokens.NextToken();
					if (!ok)
					{
						break;
					}
					if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
					{
						ok = false;
						break;
					}
					objNumber[k] = tokens.GetIntValue();
					ok = tokens.NextToken();
					if (!ok)
					{
						break;
					}
					if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
					{
						ok = false;
						break;
					}
					address[k] = tokens.GetIntValue() + first;
				}
				if (!ok)
				{
					throw new PdfException(PdfException.ErrorReadingObjectStream);
				}
				for (int k_1 = 0; k_1 < n; ++k_1)
				{
					tokens.Seek(address[k_1]);
					tokens.NextToken();
					PdfObject obj;
					if (tokens.GetTokenType() == PdfTokenizer.TokenType.Number)
					{
						obj = new PdfNumber(tokens.GetByteContent());
					}
					else
					{
						tokens.Seek(address[k_1]);
						obj = ReadObject(false, true);
					}
					PdfIndirectReference reference = pdfDocument.GetXref().Get(objNumber[k_1]);
					// Check if this object has no incremental updates (e.g. no append mode)
					if (reference.GetObjStreamNumber() == objectStreamNumber)
					{
						reference.SetRefersTo(obj);
						obj.SetIndirectReference(reference);
					}
				}
				objectStream.GetIndirectReference().SetState(PdfObject.ORIGINAL_OBJECT_STREAM);
			}
			finally
			{
				tokens = saveTokens;
			}
		}

		protected internal virtual PdfObject ReadObject(PdfIndirectReference reference)
		{
			return ReadObject(reference, true);
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual PdfObject ReadObject(bool readAsDirect)
		{
			return ReadObject(readAsDirect, false);
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual PdfObject ReadObject(bool readAsDirect, bool objStm)
		{
			tokens.NextValidToken();
			PdfTokenizer.TokenType type = tokens.GetTokenType();
			switch (type)
			{
				case PdfTokenizer.TokenType.StartDic:
				{
					PdfDictionary dict = ReadDictionary(objStm);
					long pos = tokens.GetPosition();
					// be careful in the trailer. May not be a "next" token.
					bool hasNext;
					do
					{
						hasNext = tokens.NextToken();
					}
					while (hasNext && tokens.GetTokenType() == PdfTokenizer.TokenType.Comment);
					if (hasNext && tokens.TokenValueEqualsTo(PdfTokenizer.Stream))
					{
						//skip whitespaces
						int ch;
						do
						{
							ch = tokens.Read();
						}
						while (ch == 32 || ch == 9 || ch == 0 || ch == 12);
						if (ch != '\n')
						{
							ch = tokens.Read();
						}
						if (ch != '\n')
						{
							tokens.BackOnePosition(ch);
						}
						return new PdfStream(tokens.GetPosition(), dict);
					}
					else
					{
						tokens.Seek(pos);
						return dict;
					}
					goto case PdfTokenizer.TokenType.StartArray;
				}

				case PdfTokenizer.TokenType.StartArray:
				{
					return ReadArray(objStm);
				}

				case PdfTokenizer.TokenType.Number:
				{
					return new PdfNumber(tokens.GetByteContent());
				}

				case PdfTokenizer.TokenType.String:
				{
					PdfString pdfString = new PdfString(tokens.GetByteContent(), tokens.IsHexString()
						);
					if (currentIndirectReference != null)
					{
						pdfString.SetDecryptInfoNum(currentIndirectReference.GetObjNumber());
						pdfString.SetDecryptInfoGen(currentIndirectReference.GetGenNumber());
					}
					return properties.password == null || objStm ? pdfString : pdfString.Decrypt(decrypt
						);
				}

				case PdfTokenizer.TokenType.Name:
				{
					return ReadPdfName(readAsDirect);
				}

				case PdfTokenizer.TokenType.Ref:
				{
					int num = tokens.GetObjNr();
					PdfXrefTable table = pdfDocument.GetXref();
					PdfIndirectReference reference = table.Get(num);
					if (reference != null)
					{
						if (reference.IsFree())
						{
							return PdfNull.PDF_NULL;
						}
						if (reference.GetGenNumber() != tokens.GetGenNr())
						{
							if (fixedXref)
							{
								ILogger logger = LoggerFactory.GetLogger(typeof(iTextSharp.Kernel.Pdf.PdfReader));
								logger.Warn(String.Format(LogMessageConstant.INVALID_INDIRECT_REFERENCE + " {0} {1} R"
									, tokens.GetObjNr(), tokens.GetGenNr()));
								return new PdfNull();
							}
							else
							{
								throw new PdfException(PdfException.InvalidIndirectReference1);
							}
						}
					}
					else
					{
						reference = table.Add(((PdfIndirectReference)new PdfIndirectReference(pdfDocument
							, num, tokens.GetGenNr(), 0).SetState(PdfObject.READING)));
					}
					return reference;
				}

				case PdfTokenizer.TokenType.EndOfFile:
				{
					throw new PdfException(PdfException.UnexpectedEndOfFile);
				}

				default:
				{
					if (tokens.TokenValueEqualsTo(PdfTokenizer.Null))
					{
						if (readAsDirect)
						{
							return PdfNull.PDF_NULL;
						}
						else
						{
							return new PdfNull();
						}
					}
					else
					{
						if (tokens.TokenValueEqualsTo(PdfTokenizer.True))
						{
							if (readAsDirect)
							{
								return PdfBoolean.TRUE;
							}
							else
							{
								return new PdfBoolean(true);
							}
						}
						else
						{
							if (tokens.TokenValueEqualsTo(PdfTokenizer.False))
							{
								if (readAsDirect)
								{
									return PdfBoolean.FALSE;
								}
								else
								{
									return new PdfBoolean(false);
								}
							}
						}
					}
					return null;
				}
			}
		}

		protected internal virtual PdfName ReadPdfName(bool readAsDirect)
		{
			if (readAsDirect)
			{
				PdfName cachedName = PdfName.staticNames[tokens.GetStringValue()];
				if (cachedName != null)
				{
					return cachedName;
				}
			}
			// an indirect name (how odd...), or a non-standard one
			return new PdfName(tokens.GetByteContent());
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual PdfDictionary ReadDictionary(bool objStm)
		{
			PdfDictionary dic = new PdfDictionary();
			while (true)
			{
				tokens.NextValidToken();
				if (tokens.GetTokenType() == PdfTokenizer.TokenType.EndDic)
				{
					break;
				}
				if (tokens.GetTokenType() != PdfTokenizer.TokenType.Name)
				{
					tokens.ThrowError(PdfException.DictionaryKey1IsNotAName, tokens.GetStringValue());
				}
				PdfName name = ReadPdfName(true);
				PdfObject obj = ReadObject(true, objStm);
				if (obj == null)
				{
					if (tokens.GetTokenType() == PdfTokenizer.TokenType.EndDic)
					{
						tokens.ThrowError(PdfException.UnexpectedGtGt);
					}
					if (tokens.GetTokenType() == PdfTokenizer.TokenType.EndArray)
					{
						tokens.ThrowError(PdfException.UnexpectedCloseBracket);
					}
				}
				dic.Put(name, obj);
			}
			return dic;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual PdfArray ReadArray(bool objStm)
		{
			PdfArray array = new PdfArray();
			while (true)
			{
				PdfObject obj = ReadObject(true, objStm);
				if (obj == null)
				{
					if (tokens.GetTokenType() == PdfTokenizer.TokenType.EndArray)
					{
						break;
					}
					if (tokens.GetTokenType() == PdfTokenizer.TokenType.EndDic)
					{
						tokens.ThrowError(PdfException.UnexpectedGtGt);
					}
				}
				array.Add(obj);
			}
			return array;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void ReadXref()
		{
			tokens.Seek(tokens.GetStartxref());
			tokens.NextToken();
			if (!tokens.TokenValueEqualsTo(PdfTokenizer.Startxref))
			{
				throw new PdfException(PdfException.PdfStartxrefNotFound, tokens);
			}
			tokens.NextToken();
			if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
			{
				throw new PdfException(PdfException.PdfStartxrefIsNotFollowedByANumber, tokens);
			}
			long startxref = tokens.GetLongValue();
			lastXref = startxref;
			eofPos = tokens.GetPosition();
			try
			{
				if (ReadXrefStream(startxref))
				{
					xrefStm = true;
					return;
				}
			}
			catch (Exception)
			{
			}
			// clear xref because of possible issues at reading xref stream.
			pdfDocument.GetXref().Clear();
			tokens.Seek(startxref);
			trailer = ReadXrefSection();
			//  Prev key - integer value
			//  (Present only if the file has more than one cross-reference section; shall be an indirect reference)
			// The byte offset in the decoded stream from the beginning of the file
			// to the beginning of the previous cross-reference section.
			PdfDictionary trailer2 = trailer;
			while (true)
			{
				PdfNumber prev = (PdfNumber)trailer2.Get(PdfName.Prev);
				if (prev == null)
				{
					break;
				}
				if (prev.LongValue() == startxref)
				{
					throw new PdfException(PdfException.TrailerPrevEntryPointsToItsOwnCrossReferenceSection
						);
				}
				startxref = prev.LongValue();
				tokens.Seek(startxref);
				trailer2 = ReadXrefSection();
			}
			int xrefSize = trailer.GetAsInt(PdfName.Size);
			if (xrefSize == null || xrefSize != pdfDocument.GetXref().Size())
			{
				throw new PdfException(PdfException.InvalidXrefTable);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual PdfDictionary ReadXrefSection()
		{
			tokens.NextValidToken();
			if (!tokens.TokenValueEqualsTo(PdfTokenizer.Xref))
			{
				tokens.ThrowError(PdfException.XrefSubsectionNotFound);
			}
			PdfXrefTable xref = pdfDocument.GetXref();
			while (true)
			{
				tokens.NextValidToken();
				if (tokens.TokenValueEqualsTo(PdfTokenizer.Trailer))
				{
					break;
				}
				if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
				{
					tokens.ThrowError(PdfException.ObjectNumberOfTheFirstObjectInThisXrefSubsectionNotFound
						);
				}
				int start = tokens.GetIntValue();
				tokens.NextValidToken();
				if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
				{
					tokens.ThrowError(PdfException.NumberOfEntriesInThisXrefSubsectionNotFound);
				}
				int end = tokens.GetIntValue() + start;
				for (int num = start; num < end; num++)
				{
					tokens.NextValidToken();
					long pos = tokens.GetLongValue();
					tokens.NextValidToken();
					int gen = tokens.GetIntValue();
					tokens.NextValidToken();
					PdfIndirectReference reference = xref.Get(num);
					if (reference == null)
					{
						reference = new PdfIndirectReference(pdfDocument, num, gen, pos);
					}
					else
					{
						if (reference.CheckState(PdfObject.READING) && reference.GetGenNumber() == gen)
						{
							reference.SetOffset(pos);
							reference.ClearState(PdfObject.READING);
						}
						else
						{
							continue;
						}
					}
					if (tokens.TokenValueEqualsTo(PdfTokenizer.N))
					{
						if (xref.Get(num) == null)
						{
							if (pos == 0)
							{
								tokens.ThrowError(PdfException.FilePosition0CrossReferenceEntryInThisXrefSubsection
									);
							}
							xref.Add(reference);
						}
					}
					else
					{
						if (tokens.TokenValueEqualsTo(PdfTokenizer.F))
						{
							if (xref.Get(num) == null)
							{
								reference.SetFree();
								xref.Add(reference);
							}
						}
						else
						{
							tokens.ThrowError(PdfException.InvalidCrossReferenceEntryInThisXrefSubsection);
						}
					}
				}
			}
			PdfDictionary trailer = (PdfDictionary)ReadObject(false);
			PdfObject xrs = trailer.Get(PdfName.XRefStm);
			if (xrs != null && xrs.GetType() == PdfObject.NUMBER)
			{
				int loc = ((PdfNumber)xrs).IntValue();
				try
				{
					ReadXrefStream(loc);
					xrefStm = true;
					hybridXref = true;
				}
				catch (System.IO.IOException e)
				{
					xref.Clear();
					throw;
				}
			}
			return trailer;
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual bool ReadXrefStream(long ptr)
		{
			tokens.Seek(ptr);
			if (!tokens.NextToken())
			{
				return false;
			}
			if (tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
			{
				return false;
			}
			if (!tokens.NextToken() || tokens.GetTokenType() != PdfTokenizer.TokenType.Number)
			{
				return false;
			}
			if (!tokens.NextToken() || !tokens.TokenValueEqualsTo(PdfTokenizer.Obj))
			{
				return false;
			}
			PdfXrefTable xref = pdfDocument.GetXref();
			PdfObject @object = ReadObject(false);
			PdfStream xrefStream;
			if (@object.GetType() == PdfObject.STREAM)
			{
				xrefStream = (PdfStream)@object;
				if (!PdfName.XRef.Equals(xrefStream.Get(PdfName.Type)))
				{
					return false;
				}
			}
			else
			{
				return false;
			}
			if (trailer == null)
			{
				trailer = new PdfDictionary();
				trailer.PutAll(xrefStream);
				trailer.Remove(PdfName.DecodeParms);
				trailer.Remove(PdfName.Filter);
				trailer.Remove(PdfName.Prev);
				trailer.Remove(PdfName.Length);
			}
			int size = ((PdfNumber)xrefStream.Get(PdfName.Size)).IntValue();
			PdfArray index;
			PdfObject obj = xrefStream.Get(PdfName.Index);
			if (obj == null)
			{
				index = new PdfArray();
				index.Add(new PdfNumber(0));
				index.Add(new PdfNumber(size));
			}
			else
			{
				index = (PdfArray)obj;
			}
			PdfArray w = xrefStream.GetAsArray(PdfName.W);
			long prev = -1;
			obj = xrefStream.Get(PdfName.Prev);
			if (obj != null)
			{
				prev = ((PdfNumber)obj).LongValue();
			}
			xref.SetCapacity(size);
			byte[] b = ReadStreamBytes(xrefStream, true);
			int bptr = 0;
			int[] wc = new int[3];
			for (int k = 0; k < 3; ++k)
			{
				wc[k] = w.GetAsNumber(k).IntValue();
			}
			for (int idx = 0; idx < index.Size(); idx += 2)
			{
				int start = index.GetAsNumber(idx).IntValue();
				int length = index.GetAsNumber(idx + 1).IntValue();
				xref.SetCapacity(start + length);
				while (length-- > 0)
				{
					int type = 1;
					if (wc[0] > 0)
					{
						type = 0;
						for (int k_1 = 0; k_1 < wc[0]; ++k_1)
						{
							type = (type << 8) + (b[bptr++] & 0xff);
						}
					}
					long field2 = 0;
					for (int k_2 = 0; k_2 < wc[1]; ++k_2)
					{
						field2 = (field2 << 8) + (b[bptr++] & 0xff);
					}
					int field3 = 0;
					for (int k_3 = 0; k_3 < wc[2]; ++k_3)
					{
						field3 = (field3 << 8) + (b[bptr++] & 0xff);
					}
					int @base = start;
					PdfIndirectReference newReference;
					switch (type)
					{
						case 0:
						{
							if (@base == 0)
							{
								//indirect reference with number = 0 can't be overridden
								//xref table already has indirect reference 0 65535 R
								newReference = xref.Get(@base);
							}
							else
							{
								newReference = new PdfIndirectReference(pdfDocument, @base, field3, 0);
								newReference.SetFree();
							}
							break;
						}

						case 1:
						{
							newReference = new PdfIndirectReference(pdfDocument, @base, field3, field2);
							break;
						}

						case 2:
						{
							newReference = new PdfIndirectReference(pdfDocument, @base, 0, field3);
							newReference.SetObjStreamNumber((int)field2);
							break;
						}

						default:
						{
							throw new PdfException(PdfException.InvalidXrefStream);
						}
					}
					if (xref.Get(@base) == null)
					{
						xref.Add(newReference);
					}
					else
					{
						if (xref.Get(@base).CheckState(PdfObject.READING) && xref.Get(@base).GetObjNumber
							() == newReference.GetObjNumber() && xref.Get(@base).GetGenNumber() == newReference
							.GetGenNumber())
						{
							PdfIndirectReference reference = xref.Get(@base);
							reference.SetOffset(newReference.GetOffset());
							reference.SetObjStreamNumber(newReference.GetObjStreamNumber());
							reference.ClearState(PdfObject.READING);
						}
					}
					++start;
				}
			}
			return prev == -1 || ReadXrefStream(prev);
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void FixXref()
		{
			fixedXref = true;
			PdfXrefTable xref = pdfDocument.GetXref();
			tokens.Seek(0);
			ByteBuffer buffer = new ByteBuffer(24);
			PdfTokenizer lineTokeniser = new PdfTokenizer(new RandomAccessFileOrArray(new PdfReader.ReusableRandomAccessSource
				(buffer)));
			for (; ; )
			{
				long pos = tokens.GetPosition();
				buffer.Reset();
				if (!tokens.ReadLineSegment(buffer, true))
				{
					// added boolean because of mailing list issue (17 Feb. 2014)
					break;
				}
				if (buffer.Get(0) >= '0' && ((sbyte)buffer.Get(0)) <= '9')
				{
					int[] obj = PdfTokenizer.CheckObjectStart(lineTokeniser);
					if (obj == null)
					{
						continue;
					}
					int num = obj[0];
					int gen = obj[1];
					PdfIndirectReference reference = xref.Get(num);
					if (reference != null && reference.GetGenNumber() == gen)
					{
						reference.FixOffset(pos);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal virtual void RebuildXref()
		{
			xrefStm = false;
			hybridXref = false;
			rebuiltXref = true;
			PdfXrefTable xref = pdfDocument.GetXref();
			xref.Clear();
			tokens.Seek(0);
			trailer = null;
			ByteBuffer buffer = new ByteBuffer(24);
			PdfTokenizer lineTokeniser = new PdfTokenizer(new RandomAccessFileOrArray(new PdfReader.ReusableRandomAccessSource
				(buffer)));
			for (; ; )
			{
				long pos = tokens.GetPosition();
				buffer.Reset();
				if (!tokens.ReadLineSegment(buffer, true))
				{
					// added boolean because of mailing list issue (17 Feb. 2014)
					break;
				}
				if (buffer.Get(0) == 't')
				{
					if (!PdfTokenizer.CheckTrailer(buffer))
					{
						continue;
					}
					tokens.Seek(pos);
					tokens.NextToken();
					pos = tokens.GetPosition();
					try
					{
						PdfDictionary dic = (PdfDictionary)ReadObject(false);
						if (dic.Get(PdfName.Root, false) != null)
						{
							trailer = dic;
						}
						else
						{
							tokens.Seek(pos);
						}
					}
					catch (Exception)
					{
						tokens.Seek(pos);
					}
				}
				else
				{
					if (buffer.Get(0) >= '0' && ((sbyte)buffer.Get(0)) <= '9')
					{
						int[] obj = PdfTokenizer.CheckObjectStart(lineTokeniser);
						if (obj == null)
						{
							continue;
						}
						int num = obj[0];
						int gen = obj[1];
						if (xref.Get(num) == null || xref.Get(num).GetGenNumber() <= gen)
						{
							xref.Add(new PdfIndirectReference(pdfDocument, num, gen, pos));
						}
					}
				}
			}
			if (trailer == null)
			{
				throw new PdfException(PdfException.TrailerNotFound);
			}
		}

		public virtual byte[] GetOriginalFileId()
		{
			PdfArray id = trailer.GetAsArray(PdfName.ID);
			if (id != null)
			{
				return ByteUtils.GetIsoBytes(id.GetAsString(0).GetValue());
			}
			else
			{
				return PdfEncryption.GenerateNewDocumentId();
			}
		}

		public virtual bool IsEncrypted()
		{
			return encrypted;
		}

		/// <summary>Utility method that checks the provided byte source to see if it has junk bytes at the beginning.
		/// 	</summary>
		/// <remarks>
		/// Utility method that checks the provided byte source to see if it has junk bytes at the beginning.  If junk bytes
		/// are found, construct a tokeniser that ignores the junk.  Otherwise, construct a tokeniser for the byte source as it is
		/// </remarks>
		/// <param name="byteSource">the source to check</param>
		/// <returns>a tokeniser that is guaranteed to start at the PDF header</returns>
		/// <exception cref="System.IO.IOException">if there is a problem reading the byte source
		/// 	</exception>
		private static PdfTokenizer GetOffsetTokeniser(IRandomAccessSource byteSource)
		{
			PdfTokenizer tok = new PdfTokenizer(new RandomAccessFileOrArray(byteSource));
			int offset = tok.GetHeaderOffset();
			if (offset != 0)
			{
				IRandomAccessSource offsetSource = new WindowRandomAccessSource(byteSource, offset
					);
				tok = new PdfTokenizer(new RandomAccessFileOrArray(offsetSource));
			}
			return tok;
		}

		private PdfObject ReadObject(PdfIndirectReference reference, bool fixXref)
		{
			if (reference == null)
			{
				return null;
			}
			if (reference.refersTo != null)
			{
				return reference.refersTo;
			}
			try
			{
				currentIndirectReference = reference;
				if (reference.GetObjStreamNumber() > 0)
				{
					PdfStream objectStream = (PdfStream)pdfDocument.GetXref().Get(reference.GetObjStreamNumber
						()).GetRefersTo(false);
					ReadObjectStream(objectStream);
					return reference.refersTo;
				}
				else
				{
					if (reference.GetOffset() > 0)
					{
						PdfObject @object;
						try
						{
							tokens.Seek(reference.GetOffset());
							tokens.NextValidToken();
							if (tokens.GetTokenType() != PdfTokenizer.TokenType.Obj || tokens.GetObjNr() != reference
								.GetObjNumber() || tokens.GetGenNr() != reference.GetGenNumber())
							{
								tokens.ThrowError(PdfException.InvalidOffsetForObject1, reference.ToString());
							}
							@object = ReadObject(false);
						}
						catch (Exception ex)
						{
							if (fixXref && reference.GetObjStreamNumber() == 0)
							{
								FixXref();
								@object = ReadObject(reference, false);
							}
							else
							{
								throw;
							}
						}
						return @object != null ? @object.SetIndirectReference(reference) : null;
					}
					else
					{
						return null;
					}
				}
			}
			catch (System.IO.IOException e)
			{
				throw new PdfException(PdfException.CannotReadPdfObject, e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void CheckPdfStreamLength(PdfStream pdfStream)
		{
			if (!correctStreamLength)
			{
				return;
			}
			long fileLength = tokens.Length();
			long start = pdfStream.GetOffset();
			bool calc = false;
			int streamLength = 0;
			PdfNumber pdfNumber = pdfStream.GetAsNumber(PdfName.Length);
			if (pdfNumber != null)
			{
				streamLength = pdfNumber.IntValue();
				if (streamLength + start > fileLength - 20)
				{
					calc = true;
				}
				else
				{
					tokens.Seek(start + streamLength);
					String line = tokens.ReadString(20);
					if (!line.StartsWith(endstream2) && !line.StartsWith(endstream3) && !line.StartsWith
						(endstream4) && !line.StartsWith(endstream1))
					{
						calc = true;
					}
				}
			}
			else
			{
				pdfNumber = new PdfNumber(0);
				pdfStream.Put(PdfName.Length, pdfNumber);
				calc = true;
			}
			if (calc)
			{
				ByteBuffer line = new ByteBuffer(16);
				tokens.Seek(start);
				long pos;
				while (true)
				{
					pos = tokens.GetPosition();
					line.Reset();
					if (!tokens.ReadLineSegment(line, false))
					{
						// added boolean because of mailing list issue (17 Feb. 2014)
						break;
					}
					if (line.StartsWith(endstream))
					{
						streamLength = (int)(pos - start);
						break;
					}
					else
					{
						if (line.StartsWith(endobj))
						{
							tokens.Seek(pos - 16);
							String s = tokens.ReadString(16);
							int index = s.IndexOf(endstream1);
							if (index >= 0)
							{
								pos = pos - 16 + index;
							}
							streamLength = (int)(pos - start);
							break;
						}
					}
				}
				tokens.Seek(pos - 2);
				if (tokens.Read() == 13)
				{
					streamLength--;
				}
				tokens.Seek(pos - 1);
				if (tokens.Read() == 10)
				{
					streamLength--;
				}
				pdfNumber.SetValue(streamLength);
				pdfStream.UpdateLength(streamLength);
			}
		}

		/// <summary>This method is invoked while deserialization</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			if (sourcePath != null && tokens == null)
			{
				tokens = GetOffsetTokeniser(new RandomAccessSourceFactory().SetForceRead(false).CreateBestSource
					(sourcePath));
			}
		}

		/// <summary>This method is invoked while serialization</summary>
		/// <exception cref="System.IO.IOException"/>
		private void WriteObject(ObjectOutputStream @out)
		{
			if (sourcePath != null)
			{
				PdfTokenizer tempTokens = tokens;
				tokens = null;
				@out.DefaultWriteObject();
				tokens = tempTokens;
			}
			else
			{
				@out.DefaultWriteObject();
			}
		}

		protected internal class ReusableRandomAccessSource : IRandomAccessSource
		{
			private ByteBuffer buffer;

			public ReusableRandomAccessSource(ByteBuffer buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException();
				}
				this.buffer = buffer;
			}

			public virtual int Get(long offset)
			{
				if (offset >= buffer.Size())
				{
					return -1;
				}
				return 0xff & buffer.GetInternalBuffer()[(int)offset];
			}

			public virtual int Get(long offset, byte[] bytes, int off, int len)
			{
				if (buffer == null)
				{
					throw new InvalidOperationException("Already closed");
				}
				if (offset >= buffer.Size())
				{
					return -1;
				}
				if (offset + len > buffer.Size())
				{
					len = (int)(buffer.Size() - offset);
				}
				System.Array.Copy(buffer.GetInternalBuffer(), (int)offset, bytes, off, len);
				return len;
			}

			public virtual long Length()
			{
				return buffer.Size();
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void Close()
			{
				buffer = null;
			}
		}
	}
}