using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ME.Common.Helpers
{
    public class WebProxyConverter : JsonConverter<WebProxy?>
    {
        public override WebProxy? ReadJson(JsonReader reader, Type objectType, WebProxy? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var proxyString = (string?)reader.Value;

            if (proxyString is not null)
            {
                var address = string.Empty;
                var bypasslocalhost = true;
                List<string> bypass = new();
                var username = string.Empty;
                var password = string.Empty;

                if (proxyString.ToLower() == "off")
                {
                    return new WebProxy();
                }
                else
                {

                    foreach (var keyvalue in proxyString.Split(new char[] { ';' }))
                    {
                        var data = keyvalue.Split(new char[] { '=' });
                        if (data.Length < 2)
                        {
                            continue;
                        }

                        switch (data[0].ToLower().Trim())
                        {
                            case "address": address = data[1]; break;
                            case "bypasslocalhost": if (!bool.TryParse(data[1], out bypasslocalhost)) { bypasslocalhost = false; } break;
                            case "bypass": bypass.Add(data[1]); break;
                            case "username": username = data[1]; break;
                            case "password": password = data[1]; break;
                            default: break;
                        }
                    }

                    WebProxy? proxy;

                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        proxy = new WebProxy(address, bypasslocalhost, bypass.ToArray());
                    }
                    else
                    {
                        return null;
                    }

                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        proxy.UseDefaultCredentials = false;
                        proxy.Credentials = new NetworkCredential(username, password);
                    }

                    return proxy;
                }
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, WebProxy? value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}