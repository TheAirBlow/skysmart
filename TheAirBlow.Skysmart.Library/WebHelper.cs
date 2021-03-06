using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace TheAirBlow.Skysmart.Library
{
    public static class WebHelper
    {
        /// <summary>
        /// Information JSON
        /// </summary>
        public class UserInformation
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("surname")] public string Surname;
        }
        
        /// <summary>
        /// XML content JSON
        /// </summary>
        public class ExerciseXml
        {
            public XmlDocument XmlContent;
            [JsonProperty("uuid")] public string Uuid;
            [JsonProperty("title")] public string Title;
            [JsonProperty("content")] public string Content;
            [JsonProperty("isRandom")] public bool IsRandom;
            [JsonProperty("isInteractive")] public bool IsInteractive;
            [JsonProperty("stepRevId")] public int ExerciseIdentifier;
        }
        
        /// <summary>
        /// Exercises' UUIDs JSON
        /// </summary>
        public class ExerciseMeta
        {
            public class MetaClass {
                public class TitleClass {
                    [JsonProperty("title")] public string Title;
                }

                [JsonProperty("stepUuids")] public string[] Uuids;
                [JsonProperty("subject")] public TitleClass Subject;
                [JsonProperty("teacher")] public UserInformation TeacherInformation;
                [JsonProperty("stepsMeta")] public Dictionary<string, TitleClass> StepsMeta;
                
            }

            [JsonProperty("meta")] public MetaClass Meta;
        }
        
        /// <summary>
        /// Login/password pair JSON
        /// </summary>
        private class LoginPasswordPair
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
        public static UserInformation GetInformation()
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var answer = client.DownloadString(Information);
            return JsonConvert.DeserializeObject<UserInformation>(answer);
        }

        /// <summary>
        /// Authenticate (login)
        /// </summary>
        public static void Authenticate(string login, string password)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Accept, "application/json; charset=UTF-8");
            var data = JsonConvert.SerializeObject(new LoginPasswordPair {
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
        public static ExerciseMeta GetAnswerXmlsUuids(string taskHash)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var data = "{\"taskHash\":\"" + taskHash + "\"}";
            var answer = client.UploadString(Preview, "POST", data);
            var json = JsonConvert.DeserializeObject<ExerciseMeta>(answer);
            return json;
        }

        /// <summary>
        /// Get answer XML from UUID
        /// </summary>
        /// <param name="uuid">UUID</param>
        /// <param name="uuids">UUIDs Meta</param>
        /// <returns>Answer XML</returns>
        public static ExerciseXml GetAnswerXml(string uuid, ExerciseMeta uuids)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}");
            var answer = client.DownloadString(Xml + uuid);
            var json = JsonConvert.DeserializeObject<ExerciseXml>(answer);
            var content = Cleanup(json.Content);
            var doc = new XmlDocument();
            doc.LoadXml(content);
            json.XmlContent = doc;
            json.Title = uuids.Meta.StepsMeta[uuid].Title;
            json.Uuid = uuid;
            return json;
        }

        /// <summary>
        /// Cleanup a string
        /// </summary>
        /// <param name="content">Content</param>
        /// <returns>Cleaned up string</returns>
        public static string Cleanup(string content)
        {
            content = content.Replace("~", "");
            content = content.Replace("\r", "");
            content = content.Replace("\n", "");
            content = content.Replace("\\gt", "&gt;");
            content = content.Replace("\\lt", "&lt;");
            content = content.Replace("\\pm", "⊥");
            content = content.Replace("\\perp", "");
            content = content.Replace("\\larr", "←");
            content = content.Replace("\\leftarrow", "←");
            content = content.Replace("\\le", "≤");
            content = content.Replace("\\ge", "≥");
            content = content.Replace("\\begin{cases}", "{");
            content = content.Replace("\\end{cases}", "}");
            content = content.Replace("\\cdot", " x");
            content = content.Replace("\\rarr", "→");
            content = content.Replace("\\rightarrow", "→");
            content = content.Replace("\\pi", "π");
            content = content.Replace("\\infty", "∞");

            foreach (Match i in new Regex("mathrm{(.*?)}").Matches(content))
                content = content.Replace("\\" + i.Value, Regex.Replace(
                    i.Value, "(?:mathrm{)(.*?)(?:})", "$1"));
            
            foreach (Match i in new Regex("dfrac{(.*?)}{(.*?)}").Matches(content))
                content = content.Replace("\\" + i.Value, Regex.Replace(
                    i.Value, "(?:dfrac{)(.*?)(?:}{)(.*?)(?:})",
                    "$1/$2 (дробь)"));
            
            foreach (Match i in new Regex("sqrt{(.*?)}").Matches(content))
                content = content.Replace("\\" + i.Value, Regex.Replace(
                    i.Value, "(?:sqrt){(.*?)(?:})", "√$1"));
            return content;
        }
    }
}