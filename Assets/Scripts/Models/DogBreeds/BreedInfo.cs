using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SwiftRiver.Models.DogBreeds
{
    [Serializable]
    public class BreedInfo
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("name")]
        public string name;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("temperament")]
        public string temperament;

        [JsonProperty("origin")]
        public string origin;

        [JsonProperty("lifeSpan")]
        public string lifeSpan;

        [JsonProperty("heightMetric")]
        public string heightMetric;

        [JsonProperty("weightMetric")]
        public string weightMetric;

        public static BreedInfo FromAttributes(string breedId, DogBreedAttributes attr)
        {
            if (attr == null)
                return null;
            string life = attr.Life != null ? $"{attr.Life.Min} - {attr.Life.Max} years" : string.Empty;
            string weight = attr.MaleWeight != null
                ? $"{attr.MaleWeight.Min} - {attr.MaleWeight.Max} kg"
                : (attr.FemaleWeight != null ? $"{attr.FemaleWeight.Min} - {attr.FemaleWeight.Max} kg" : string.Empty);
            return new BreedInfo
            {
                id = breedId,
                name = attr.Name ?? string.Empty,
                description = attr.Description ?? string.Empty,
                temperament = string.Empty,
                origin = string.Empty,
                lifeSpan = life,
                heightMetric = string.Empty,
                weightMetric = weight,
            };
        }
    }

    [Serializable]
    public class DogBreedsListResponse
    {
        [JsonProperty("data")]
        public List<DogBreedData> Data;
    }

    [Serializable]
    public class DogBreedSingleResponse
    {
        [JsonProperty("data")]
        public DogBreedData Data;
    }

    [Serializable]
    public class DogBreedData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("attributes")]
        public DogBreedAttributes Attributes;
    }

    [Serializable]
    public class DogBreedAttributes
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("life")]
        public DogRange Life;

        [JsonProperty("male_weight")]
        public DogRange MaleWeight;

        [JsonProperty("female_weight")]
        public DogRange FemaleWeight;

        [JsonProperty("hypoallergenic")]
        public bool Hypoallergenic;
    }

    [Serializable]
    public class DogRange
    {
        [JsonProperty("min")]
        public float Min;

        [JsonProperty("max")]
        public float Max;
    }

    [Serializable]
    public class FactInfo
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("breed")]
        public string breed;

        [JsonProperty("content")]
        public string content;
    }
}
