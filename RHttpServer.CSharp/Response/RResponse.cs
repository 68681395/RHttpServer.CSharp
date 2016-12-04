using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RHttpServer.Logging;
using RHttpServer.Plugins;

namespace RHttpServer.Response
{
    /// <summary>
    ///     Class representing the reponse to a clients request
    ///     All 
    /// </summary>
    public class RResponse
    {
        private const int BufferSize = 0x1000;

        internal static readonly IDictionary<string, string> MimeTypes =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".json", "text/json"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mp4", "video/mp4"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".php", "text/x-php"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"}
                #endregion
            };

        internal RResponse(HttpListenerResponse res, RPluginCollection rPluginCollection)
        {
            UnderlyingResponse = res;
            Plugins = rPluginCollection;
        }
        
        /// <summary>
        ///     The plugins registered to the server
        /// </summary>
        public RPluginCollection Plugins { get; }


        /// <summary>
        ///     The underlying HttpListenerResponse <para/>
        ///     The implementation of RResponse is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerResponse UnderlyingResponse { get; }

        /// <summary>
        ///     Add header item to response
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void AddHeader(string fieldName, string fieldValue)
        {
            UnderlyingResponse.AddHeader(fieldName, fieldValue);
        }

        /// <summary>
        ///     Redirects the client to a given path or url
        /// </summary>
        /// <param name="redirectPath">The path or url to redirect to</param>
        public void Redirect(string redirectPath)
        {
            UnderlyingResponse.Redirect(redirectPath);
            UnderlyingResponse.Close();
        }

        /// <summary>
        ///     Sends data as text
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="status">The status code for the response</param>
        public async void SendString(string data, string contentType = "text/plain",
            int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(data);
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await InternalTransfer(bytes, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends data as bytes
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="filename">The name of the origin file, if any</param>
        /// <param name="status">The status code for the response</param>
        public async void SendBytes(byte[] data, string contentType = "application/octet-stream", string filename = "",
            int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.AddHeader("Accept-Ranges", "bytes");
                UnderlyingResponse.ContentLength64 = data.Length;
                if (!string.IsNullOrEmpty(filename))
                    UnderlyingResponse.AddHeader("Content-disposition", $"inline; filename=\"{filename}\"");
                await InternalTransfer(data, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends a segment of a byte array
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="rangeStart">The offset in the array</param>
        /// <param name="rangeEnd">The position of the last byte to send</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="filename">The name of the origin file, if any</param>
        /// <param name="status">The status code for the response</param>
        public async void SendBytes(byte[] data, long rangeStart, long rangeEnd, string contentType, string filename = "", int status = (int)HttpStatusCode.PartialContent)
        {
            try
            {
                UnderlyingResponse.StatusCode = 206;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.AddHeader("Accept-Ranges", "bytes");
                var len = data.LongLength;
                var start = CalcStart(len, rangeStart, rangeEnd);
                len = CalcLength(len, start, rangeEnd);
                if (!string.IsNullOrEmpty(filename))
                    UnderlyingResponse.AddHeader("Content-disposition", $"inline; filename=\"{filename}\"");
                UnderlyingResponse.AddHeader("Content-Range", $"bytes {start}-{start + len - 1}/{start + len}");
                if (rangeEnd == -1 || rangeEnd > len)
                    rangeEnd = len;
                await InternalTransfer(data, UnderlyingResponse.OutputStream, start, rangeEnd);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends data from stream
        /// </summary>
        /// <param name="stream">Stream of data to send</param>
        /// <param name="length">Length of the content in the stream</param>
        /// <param name="gzipCompress">Whether the data should be compressed</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="filename">The name of the file, if filestream</param>
        /// <param name="status">The status code for the response</param>
        /// <exception cref="RHttpServerException"></exception>
        public async void SendFromStream(Stream stream, long length, bool gzipCompress = false, string contentType = "application/octet-stream", string filename = "",
            int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                if (gzipCompress) UnderlyingResponse.AddHeader("Content-Encoding", "gzip");
                if (!string.IsNullOrEmpty(filename))
                    UnderlyingResponse.AddHeader("Content-disposition", $"inline; filename=\"{filename}\"");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.ContentLength64 = length;
                if (!gzipCompress)
                    await InternalTransfer(stream, UnderlyingResponse.OutputStream);
                else
                    using (var zip = new GZipStream(stream, CompressionMode.Compress, false))
                        await InternalTransfer(zip, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends object serialized to text using the current IJsonConverter plugin
        /// </summary>
        /// <param name="data">The object to be serialized and send</param>
        /// <param name="status">The status code for the response</param>
        public async void SendJson(object data, int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(Plugins.Use<IJsonConverter>().Serialize(data));
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = "application/json";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await InternalTransfer(bytes, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends object serialized to text using the current IXmlConverter plugin
        /// </summary>
        /// <param name="data">The object to be serialized and send</param>
        /// <param name="status">The status code for the response</param>
        public async void SendXml(object data, int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(Plugins.Use<IXmlConverter>().Serialize(data));
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = "application/xml";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await InternalTransfer(bytes, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be displayed in-browser if possible
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public async void SendFile(string filepath, string mime = null, int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (mime == null && !MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime))
                    mime = "application/octet-stream";
                UnderlyingResponse.ContentType = mime;
                UnderlyingResponse.AddHeader("Accept-Ranges", "bytes");
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.AddHeader("Content-disposition", "inline; filename=\"" + Path.GetFileName(filepath) + "\"");
                using (Stream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var len = input.Length;
                    UnderlyingResponse.ContentLength64 = len;
                    await InternalTransfer(input, UnderlyingResponse.OutputStream);
                }
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be displayed in-browser if possible
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="rangeStart">The offset in the file</param>
        /// <param name="rangeEnd">The position of the last byte to send, in the file</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public async void SendFile(string filepath, long rangeStart, long rangeEnd, string mime = null, int status = (int)HttpStatusCode.PartialContent)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (mime == null && !MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime))
                    mime = "application/octet-stream";
                UnderlyingResponse.ContentType = mime;
                UnderlyingResponse.AddHeader("Accept-Ranges", "bytes");
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.AddHeader("Content-disposition", "inline; filename=\"" + Path.GetFileName(filepath) + "\"");
                using (Stream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var len = input.Length;
                    var start = CalcStart(len, rangeStart, rangeEnd);
                    len = CalcLength(len, start, rangeEnd);
                    UnderlyingResponse.AddHeader("Content-Range", $"bytes {start}-{start + len-1}/{start + len}");
                    UnderlyingResponse.ContentLength64 = len;
                    await InternalTransfer(input, UnderlyingResponse.OutputStream, rangeStart, (int)len);
                }
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be downloaded as an attachment
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="filename">The name filename the client receives the file with, defaults to using the actual filename</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public async void Download(string filepath, string filename = "", string mime = null,
            int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (mime == null && !MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime))
                    mime = "application/octet-stream";
                UnderlyingResponse.ContentType = mime;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.AddHeader("Content-disposition", "attachment; filename=\"" +
                    (string.IsNullOrEmpty(filename) ? Path.GetFileName(filepath) : filename) + "\"");
                using (var input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var len = input.Length;
                    UnderlyingResponse.ContentLength64 = len;
                    await InternalTransfer(input, UnderlyingResponse.OutputStream);
                }
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }

        /// <summary>
        ///     Renders a page file using the current IPageRenderer plugin
        /// </summary>
        /// <param name="pagefilepath">The path of the file to be rendered</param>
        /// <param name="parameters">The parameter collection used when replacing data</param>
        /// <param name="status">The status code for the response</param>
        public async void RenderPage(string pagefilepath, RenderParams parameters, int status = (int) HttpStatusCode.OK)
        {
            try
            {
                UnderlyingResponse.StatusCode = status;
                var data = Encoding.UTF8.GetBytes(Plugins.Use<IPageRenderer>().Render(pagefilepath, parameters));
                UnderlyingResponse.ContentType = "text/html";
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentLength64 = data.Length;
                await InternalTransfer(data, UnderlyingResponse.OutputStream);
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
            }
        }
        

        private static async Task InternalTransfer(Stream src, Stream dest)
        {
            var buffer = new byte[BufferSize];
            int nbytes;
            while ((nbytes = await src.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await dest.WriteAsync(buffer, 0, nbytes);
            await dest.FlushAsync();
            dest.Close();
        }
        
        private static async Task InternalTransfer(Stream src, Stream dest, long rangeStart, long toWrite)
        {
            var buffer = new byte[BufferSize];
            if (rangeStart != 0) src.Seek(rangeStart, SeekOrigin.Begin);
            while (toWrite > 0)
            {
                int nbytes;
                if (toWrite > BufferSize)
                {
                    nbytes = await src.ReadAsync(buffer, 0, buffer.Length);
                    await dest.WriteAsync(buffer, 0, nbytes);
                    toWrite -= nbytes;
                }
                else
                {
                    nbytes = await src.ReadAsync(buffer, 0, buffer.Length);
                    await dest.WriteAsync(buffer, 0, nbytes);
                    toWrite = 0;
                }
            }
            await dest.FlushAsync();
            dest.Close();
        }
        
        private static async Task InternalTransfer(byte[] src, Stream dest)
        {
            await InternalTransfer(src, dest, 0, src.LongLength);
        }
        
        private static async Task InternalTransfer(byte[] src, Stream dest, long start, long end)
        {
            await dest.WriteAsync(src, (int)start, (int)end);
            await dest.FlushAsync();
            dest.Close();
        }
        
        private static long CalcStart(long len, long start, long end)
        {
            if (start != -1) return start;
            return len - end;
        }
        
        private static long CalcLength(long len, long start, long end)
        {
            if (end == -1)
                return len - start;
            return (len - start) - (len - end);
        }
    }
    
}