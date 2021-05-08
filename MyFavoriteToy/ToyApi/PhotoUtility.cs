using System;
using System.IO;

namespace ToyApi
{
    public class PhotoUtility
    {
        public static string GetBase64(string fileName,string fileType)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Db/Images", fileName);
            var bytes = File.ReadAllBytes(path);
            return $"data:{fileType};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
