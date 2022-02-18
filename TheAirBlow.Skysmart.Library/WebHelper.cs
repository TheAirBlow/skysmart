using System.Net;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheAirBlow.Skysmart.Library
{
    public static class WebHelper
    {
        /// <summary>
        /// Information JSON
        /// </summary>
        public class InformationJson
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("surname")] public string Surname;
        }
        
        /// <summary>
        /// XML content JSON
        /// </summary>
        private class XmlJson
        {
            [JsonProperty("content")] public string Content;
        }
        
        /// <summary>
        /// Exercises' UUIDs JSON
        /// </summary>
        private class UuidsJson
        {
            public class MetaClass {
                [JsonProperty("stepUuids")] public string[] Uuids;
            }

            [JsonProperty("meta")] public MetaClass Meta;
        }
        
        /// <summary>
        /// Login/password pair JSON
        /// </summary>
        private class LoginPasswordJson
        {
            [JsonProperty("phoneOrEmail")] public string Login;
            [JsonProperty("password")] public string Password;
        }
        
        private const string LoginRequest = "https://api-edu.skysmart.ru/api/v2/auth/auth/student";
        private const string Xml = "https://api-edu.skysmart.ru/api/v1/content/step/load?stepUuid=";
        private const string Information = "https://api-edu.skysmart.ru/api/v1/user/config";
        private const string Preview = "https://api-edu.skysmart.ru/api/v1/task/preview";
        public static string Token = "";

        /// <summary>
        /// Get information about user
        /// </summary>
        /// <returns>Information</returns>
        public static InformationJson GetInformation()
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var answer = client.DownloadString(Information);
            return JsonConvert.DeserializeObject<InformationJson>(answer);
        }

        /// <summary>
        /// Authenticate (login)
        /// </summary>
        public static void Authenticate(string login, string password)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Accept, "application/json; charset=UTF-8");
            var data = JsonConvert.SerializeObject(new LoginPasswordJson {
                Password = password,
                Login = login
            });
            var answer = client.UploadString(LoginRequest, "POST", data);
            dynamic json = JObject.Parse(answer);
            Token = json.jwtToken;
        }

        /// <summary>
        /// Get all exercises' XML UUIDs
        /// </summary>
        /// <param name="taskHash">Task Hash</param>
        /// <returns>UUIDs</returns>
        public static string[] GetAnswerXmlsUuids(string taskHash)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var data = "{\"taskHash\":\"" + taskHash + "\"}";
            var answer = client.UploadString(Preview, "POST", data);
            var json = JsonConvert.DeserializeObject<UuidsJson>(answer);
            return json.Meta.Uuids;
        }
        
        /// <summary>
        /// Get answer XML from UUID
        /// </summary>
        /// <param name="uuid">UUID</param>
        /// <returns>Answer XML</returns>
        public static XmlDocument GetAnswerXml(string uuid)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var answer = client.DownloadString(Xml + uuid);
            var json = JsonConvert.DeserializeObject<XmlJson>(answer);
            var content = json.Content;
            content = content.Replace("\r", "");
            content = content.Replace("\n", "");
            var doc = new XmlDocument();
            doc.LoadXml(content);
            return doc;
        }
    }
}