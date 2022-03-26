using System.Net;

namespace Vk2Tg.Http
{
    public static class HttpListenerResponseExtentions
    {
        public static async Task WriteString(this HttpListenerResponse response, string text)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
        }

        public static async Task ReturnOk(this HttpListenerResponse response, string message = "ok")
        {
            response.StatusCode = 200;
            await response.WriteString(message);
            response.OutputStream.Close();
            response.Close();
        }

        public static async Task ReturnBadRequest(this HttpListenerResponse response, string message = "404 bad request")
        {
            response.StatusCode = 400;
            await response.WriteString(message);
            response.OutputStream.Close();
            response.Close();
        }
    }
}