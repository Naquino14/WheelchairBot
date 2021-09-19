using System;
using SeasideResearch.LibCurlNet;
using System.IO;

namespace WheelchairBot.Modules
{
    #pragma warning disable IDE0049
    public class Curler
    {
        private readonly string path = @"curl\";
        public bool Curl(string url, out Exception ex)
        {
            string fileName = url.Split('/')[url.Split('/').Length - 1];
            try
            {
                if (File.Exists(path + fileName))
                    File.Delete(path + fileName);

                SeasideResearch.LibCurlNet.Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);
                using (Easy ez = new Easy())
                using (FileStream fs = new FileStream(path + fileName, FileMode.Create))
                {
                    Easy.WriteFunction wf = OnWriteData;
                    ez.SetOpt(CURLoption.CURLOPT_URL, url);
                    ez.SetOpt(CURLoption.CURLOPT_HTTPGET, 1);
                    ez.SetOpt(CURLoption.CURLOPT_WRITEDATA, fs);
                    ez.Perform();
                    //if (ez.Perform() == CURLcode.CURLE_OK)
                    //{ ex = null; return true; }
                    //else { ex = new Exception("Curl error. No further information."); return false; }

                }

                ex = null;
                return true;
            } catch (Exception _ex)
            {
                ex = _ex;
                return false;
            }
        }

        private Int32 OnWriteData(Byte[] buf, Int32 size, Int32 nmeb, Object extraData)
        { 
            var fs = (FileStream)extraData; 
            fs.Write(buf, 0, buf.Length); 
            return size * nmeb; 
        }
    }
}
