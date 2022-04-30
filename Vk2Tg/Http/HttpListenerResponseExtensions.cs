using System.Net;
using System.Text;

namespace Vk2Tg.Http
{
    public static class HttpListenerResponseExtensions
    {
        public static void WriteString(this HttpListenerResponse response, string text)
        {
            Span<byte> byteSpan = stackalloc byte[Encoding.UTF8.GetByteCount(text)];
            Encoding.UTF8.GetBytes(text, byteSpan);
            
            response.ContentLength64 = byteSpan.Length;
            var output = response.OutputStream;
            output.Write(byteSpan);
        }

        public static async Task ReturnOk(this HttpListenerResponse response, string message = "ok")
        {
            response.StatusCode = 200;
            response.WriteString(message);
            await response.OutputStream.DisposeAsync();
            response.Close();
        }

        public static async Task ReturnBadRequest(this HttpListenerResponse response, string message = "404 bad request")
        {
            response.StatusCode = 400;
            response.WriteString(message);
            await response.OutputStream.DisposeAsync();
            response.Close();
        }
    }
}