using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using Nethereum.Signer;
using Newtonsoft.Json;
using QRCoder;

// 0x7b380660b3e857971Ffc04a7adA5ce563aCf9f31
namespace backend
{
    class Program
    {
        class UserStatus
        {
            public string UniqueId;
            public bool LoggedIn;
            public string Token;
        }

        class FrontendLoginDto 
        {
            public string Id;
            public string QrCode;
        }

        class LoginQrDto
        {
            public string HttpAddress;
            public string Id;
        }

        class LoginRequestDto
        {
            public string Id;
            public string LoginTimestamp;
            public string LoginSignature;
        }

        static void Main(string[] args)
        {
            var userStatuses = new List<UserStatus>();
            int uniqueId = 1;
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            var httpListener = new HttpListener();

            httpListener.Prefixes.Add("http://+:666/");
            httpListener.Start();

            if (!httpListener.IsListening) 
            {
                Console.WriteLine("Failed to start");
                return;
            } 
            else 
            {
                Console.WriteLine("Listening");
            }

            while (true) 
            {
                var context = httpListener.GetContext();
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "OPTIONS")
                {
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    response.AddHeader("Access-Control-Max-Age", "1728000");
                }
                response.AppendHeader("Access-Control-Allow-Origin", "*");

                var path = context.Request.Url.LocalPath;

                Console.WriteLine($"Request: [{path}] Method: [{request.HttpMethod}]");
                
                string responseString = "";
                if (request.HttpMethod != "OPTIONS") 
                {
                    switch (path) 
                    {
                        case "/requestlogin": 
                        {
                            responseString = requestLogin(++uniqueId);
                        } break;
                        case "/login":
                        {
                            responseString = tryLogin(context.Request.InputStream, userStatuses);
                        } break;
                        case "/checklogin":
                        {
                            responseString = checkLogin(context.Request.InputStream, userStatuses);
                        } break;
                        default: 
                        {
                            responseString = "Not Found";
                        } break;
                    }
                }

                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }

        static string checkLogin(Stream stream, List<UserStatus> userStatuses) 
        {
            var inputStream = new StreamReader(stream).ReadToEnd();
            var loginCheck = JsonConvert.DeserializeObject<FrontendLoginDto>(inputStream);

            var userStatus = userStatuses.Where(x => x.UniqueId == loginCheck.Id).FirstOrDefault();

            if (userStatus != null)
                return $"Logged in; Token: {userStatus.Token}";
            else
                return "Not logged in";
        }

        static string tryLogin(Stream stream, List<UserStatus> userStatuses) 
        {
            var inputStream = new StreamReader(stream).ReadToEnd();
            var loginRequestDto = JsonConvert.DeserializeObject<LoginRequestDto>(inputStream);
            
            // getting signee
            var signerHandler = new EthereumMessageSigner();
            var recoveredAddress = signerHandler.EncodeUTF8AndEcRecover("0x" + loginRequestDto.LoginTimestamp, loginRequestDto.LoginSignature); // don't forget prefix 0x which is added by Metamask

            // hardcoded user
            if (recoveredAddress == "0x7b380660b3e857971Ffc04a7adA5ce563aCf9f31") {
                userStatuses.Add(new UserStatus {
                    UniqueId = loginRequestDto.Id,
                    LoggedIn = true,
                    Token = "UniqueToken"
                });
                return "Success";
            }
            else
                return "Failure";
        }

        static string requestLogin(int uniqueId) 
        {
            var loginQrDto = new LoginQrDto 
            {
                HttpAddress = "http://192.168.0.103:666/login",
                Id = uniqueId.ToString()
            };

            var resultJson = JsonConvert.SerializeObject(loginQrDto);

            var result = new FrontendLoginDto 
            {
                Id = uniqueId.ToString(),
                QrCode = stringToQRBase64(resultJson)
            };

            return JsonConvert.SerializeObject(result);
        }

        static string stringToQRBase64(string qrToEncode) 
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrToEncode, QRCodeGenerator.ECCLevel.L);
            var qrCode = new QRCode(qrCodeData);
            var qrSize = 10; 
            var qrCodeImage = qrCode.GetGraphic(qrSize);
            qrCodeImage.Save("./last_qr_request.jpg", ImageFormat.Jpeg);

            using var stream = new MemoryStream();
            qrCodeImage.Save(stream, ImageFormat.Jpeg);
            return Convert.ToBase64String(stream.ToArray()); 
        }
    }
}
