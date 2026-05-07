using System.Collections.Generic;
using Newtonsoft.Json;

// --- Helper classes for Newtonsoft.Json Serialization ---
// These are now public and in their own file.

public class GeminiPayload
{
    [JsonProperty("contents")]
    public List<Content> contents;
    
    [JsonProperty("systemInstruction", NullValueHandling = NullValueHandling.Ignore)]
    public Content systemInstruction;
        
}

public class Content
{
    [JsonProperty("role")]
    public string role; // "user", "model", "system", or "tool"

    [JsonProperty("parts")]
    public List<Part> parts = new List<Part>()  ;
    
    
}

public class Part
{
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string text { get; set; }

    [JsonProperty("functionCall", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionCall functionCall { get; set; }

    [JsonProperty("functionResponse", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionResponse functionResponse { get; set; }
}

public class FunctionCall
{
    [JsonProperty("name")]
    public string name { get; set; }

    [JsonProperty("args")]
    public object args { get; set; } // Will be parsed as JObject
}

public class FunctionResponse
{
    [JsonProperty("name")]
    public string name { get; set; }

    [JsonProperty("response")]
    public object response { get; set; } // Will be serialized from an anon C# object
}

public class GeminiResponse
{
    [JsonProperty("candidates")]
    public List<Candidate> candidates;
}

public class Candidate
{
    [JsonProperty("content")]
    public Content content;
}

// --- Classes to define the Tool "Menu" ---

public class Tool
{
    [JsonProperty("function_declarations")]
    public List<FunctionDeclaration> FunctionDeclarations { get; set; }
}

public class FunctionDeclaration
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("parameters")]
    public Schema Parameters { get; set; }
}

public class Schema
{
    [JsonProperty("type")]
    public string Type { get; set; } = "object";

    [JsonProperty("properties")]
    public Dictionary<string, SchemaProperty> Properties { get; set; }

    [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Required { get; set; }
}

public class SchemaProperty
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
    
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, SchemaProperty> Properties { get; set; }

    [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Required { get; set; }
}