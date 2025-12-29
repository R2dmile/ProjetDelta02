using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class Section
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public List<string> Description { get; set; } = new();
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();

        public void Afficher(IUI ui)
        {
            foreach (var ligne in Description)
            {
                ui.Afficher(ligne);
            }

            for (int i = 0; i < Choices.Count; i++)
            {
                ui.Afficher($"{i + 1}. {Choices[i].Text}");
            }
        }
        public Section() { }
        public Section(string id, List<string> description, List<Choice> choices)
        {
            Id = id;
            Description = description ?? new List<string>();
            Choices = choices ?? new List<Choice>();
        }
        public class Choice
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("nextSection")]
            public string NextSection { get; set; } = string.Empty;

            [JsonPropertyName("isCombat")]
            public bool IsCombat { get; set; }

            [JsonPropertyName("monster")]
            public string Monster { get; set; } = string.Empty;
            public Choice(string text, string nextSection, bool isCombat = false, string monster = "")
            {
                Text = text;
                NextSection = nextSection;
                IsCombat = isCombat;
                Monster = monster;
            }
        }

            public class SectionsData
            {
                [JsonPropertyName("sections")]
                public List<Section> Sections { get; set; } = new();
            }
        }

    

}
