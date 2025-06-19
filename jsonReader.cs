using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot1
{
    public class jsonReader
    {

        public string token { get; set; }
        public async Task ReadJson()
        {
            using (StreamReader sr = new StreamReader(path: "giveawaybot/data/config.json"))
            {
                string json = sr.ReadToEnd();
                jsonStructure jsonStructure = Newtonsoft.Json.JsonConvert.DeserializeObject<jsonStructure>(json);

                token = jsonStructure.token;
            }
        }
    }
    internal sealed class jsonStructure
    {
        public string token { get; set; }
    }
}
