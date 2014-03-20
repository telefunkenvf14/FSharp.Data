// --------------------------------------------------------------------------------------
// JSON type provider - methods that are called from the generated erased code
// --------------------------------------------------------------------------------------
namespace FSharp.Data.Runtime

open System
open System.ComponentModel
open System.Globalization
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes

#nowarn "10001"

/// [omit]
type IJsonDocument =
    abstract JsonValue : JsonValue
    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    abstract Path : string
    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    abstract CreateNew : value:JsonValue * pathIncrement:string -> IJsonDocument

/// Underlying representation of types generated by JsonProvider
[<StructuredFormatDisplay("{_Print}")>]
type JsonDocument = 

  private { /// [omit]
            Json : JsonValue
            /// [omit]
            Path : string }

  interface IJsonDocument with 
    member x.JsonValue = x.Json
    member x.Path = x.Path
    member x.CreateNew(value, pathIncrement) = 
        JsonDocument.Create(value, x.Path + pathIncrement)

  /// The underlying JsonValue
  member x.JsonValue = x.Json

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  member x._Print = x.Json.ToString()

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member Create(value, path) = 
    { Json = value
      Path = path } :> IJsonDocument

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member Create(reader:TextReader, cultureStr) = 
    use reader = reader
    let text = reader.ReadToEnd()
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let value = JsonValue.Parse(text, cultureInfo)
    JsonDocument.Create(value, "")

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member CreateList(reader:TextReader, cultureStr) = 
    use reader = reader
    let text = reader.ReadToEnd()
    let cultureInfo = TextRuntime.GetCulture cultureStr    
    match JsonValue.ParseMultiple(text, cultureInfo) |> Seq.toArray with
    | [| JsonValue.Array array |] -> array
    | array -> array
    |> Array.mapi (fun i value -> JsonDocument.Create(value, "[" + (string i) + "]"))

/// [omit]
type JsonValueOptionAndPath = 
  { JsonOpt : JsonValue option
    Path : string }

/// Static helper methods called from the generated code for working with JSON
type JsonRuntime = 

  // --------------------------------------------------------------------------------------
  // json option -> type

  static member ConvertString(cultureStr, json) = 
    json |> Option.bind (JsonConversions.AsString (*useNoneForNullOrWhiteSpace*)true (TextRuntime.GetCulture cultureStr))
  
  static member ConvertInteger(cultureStr, json) = 
    json |> Option.bind (JsonConversions.AsInteger (TextRuntime.GetCulture cultureStr))
  
  static member ConvertInteger64(cultureStr, json) = 
    json |> Option.bind (JsonConversions.AsInteger64 (TextRuntime.GetCulture cultureStr))

  static member ConvertDecimal(cultureStr, json) =
    json |> Option.bind (JsonConversions.AsDecimal (TextRuntime.GetCulture cultureStr))

  static member ConvertFloat(cultureStr, missingValues:string, json) = 
    json |> Option.bind (JsonConversions.AsFloat (missingValues.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)) 
                                                 (*useNoneForMissingValues*)true
                                                 (TextRuntime.GetCulture cultureStr))

  static member ConvertBoolean(cultureStr, json) = 
    json |> Option.bind (JsonConversions.AsBoolean (TextRuntime.GetCulture cultureStr))

  static member ConvertDateTime(cultureStr, json) = 
    json |> Option.bind (JsonConversions.AsDateTime (TextRuntime.GetCulture cultureStr))

  static member ConvertGuid(json) = 
    json |> Option.bind JsonConversions.AsGuid

  /// Operation that extracts the value from an option and reports a meaningful error message when the value is not there
  /// If the originalValue is a scalar, for missing strings we return "", and for missing doubles we return NaN
  /// For other types an error is thrown
  static member GetNonOptionalValue<'T>(path:string, opt:option<'T>, originalValue) : 'T = 
    let getTypeName() = 
        let name = typeof<'T>.Name
        if name.StartsWith "int" 
        then "an " + name
        else "a " + name
    match opt, originalValue with 
    | Some value, _ -> value
    | None, Some ((JsonValue.Array _ | JsonValue.Record _) as x) -> failwithf "Expecting %s at '%s', got %s" (getTypeName()) path <| x.ToString(JsonSaveOptions.DisableFormatting)
    | None, _ when typeof<'T> = typeof<string> -> "" |> unbox
    | None, _ when typeof<'T> = typeof<float> -> Double.NaN |> unbox
    | None, None -> failwithf "'%s' is missing" path
    | None, Some x -> failwithf "Expecting %s at '%s', got %s" (getTypeName()) path <| x.ToString(JsonSaveOptions.DisableFormatting)

  /// Converts JSON array to array of target types
  static member ConvertArray<'T>(doc:IJsonDocument, mapping:Func<IJsonDocument,'T>) = 
    match doc.JsonValue with     
    | JsonValue.Array elements ->
        elements
        |> Array.filter (function JsonValue.Null -> false 
                                | JsonValue.String s when s |> TextConversions.AsString |> Option.isNone -> false
                                | _ -> true)
        |> Array.mapi (fun i value -> doc.CreateNew(value, "[" + (string i) + "]") |> mapping.Invoke)
    | JsonValue.Null -> [| |]
    | x -> failwithf "Expecting an array at '%s', got %s" doc.Path <| x.ToString(JsonSaveOptions.DisableFormatting)

  /// Get optional json property
  static member TryGetPropertyUnpacked(doc:IJsonDocument, name) =
    doc.JsonValue.TryGetProperty(name)
    |> Option.bind (function JsonValue.Null -> None | x -> Some x) 

  /// Get optional json property and wrap it together with path
  static member TryGetPropertyUnpackedWithPath(doc:IJsonDocument, name) =
    { JsonOpt = JsonRuntime.TryGetPropertyUnpacked(doc, name)
      Path = doc.Path + "/" + name }

  /// Get optional json property wrapped in json document
  static member TryGetPropertyPacked(doc:IJsonDocument, name) =
    JsonRuntime.TryGetPropertyUnpacked(doc, name)
    |> Option.map (fun value -> doc.CreateNew(value, "/" + name))

  /// Get json property and wrap in json document
  static member GetPropertyPacked(doc:IJsonDocument, name) =
    match JsonRuntime.TryGetPropertyPacked(doc, name) with
    | Some doc -> doc
    | None -> failwithf "Property '%s' not found at '%s': %s" name doc.Path <| doc.JsonValue.ToString(JsonSaveOptions.DisableFormatting)

  /// Get json property and wrap in json document, and return null if not found
  static member GetPropertyPackedOrNull(doc:IJsonDocument, name) =
    match JsonRuntime.TryGetPropertyPacked(doc, name) with
    | Some doc -> doc
    | None -> doc.CreateNew(JsonValue.Null, "/" + name)

  /// Get optional json property and convert to a specified type
  static member ConvertOptionalProperty<'T>(doc:IJsonDocument, name, mapping:Func<IJsonDocument,'T>) =
    JsonRuntime.TryGetPropertyPacked(doc, name)
    |> Option.map mapping.Invoke

  static member private Matches cultureStr tag = 
    match tag with
    | InferedTypeTag.Number -> 
        let cultureInfo = TextRuntime.GetCulture cultureStr
        fun json -> (JsonConversions.AsDecimal cultureInfo json).IsSome ||
                    (JsonConversions.AsFloat [| |] (*useNoneForMissingValues*)true cultureInfo json).IsSome
    | InferedTypeTag.Boolean -> 
        JsonConversions.AsBoolean (TextRuntime.GetCulture cultureStr)
        >> Option.isSome
    | InferedTypeTag.String -> 
        JsonConversions.AsString (*useNoneForNullOrWhiteSpace*)true (TextRuntime.GetCulture cultureStr)
        >> Option.isSome
    | InferedTypeTag.DateTime -> 
        JsonConversions.AsDateTime (TextRuntime.GetCulture cultureStr)
        >> Option.isSome
    | InferedTypeTag.Guid -> 
        JsonConversions.AsGuid >> Option.isSome
    | InferedTypeTag.Collection -> 
        function JsonValue.Array _ -> true | _ -> false
    | InferedTypeTag.Record _ -> 
        function JsonValue.Record _ -> true | _ -> false
    | InferedTypeTag.Json -> 
        failwith "Json type not supported"
    | InferedTypeTag.Null -> 
        failwith "Null type not supported"
    | InferedTypeTag.Heterogeneous -> 
        failwith "Heterogeneous type not supported"

  /// Returns all array values that match the specified tag
  static member GetArrayChildrenByTypeTag<'T>(doc:IJsonDocument, cultureStr, tagCode, mapping:Func<IJsonDocument,'T>) =     
    match doc.JsonValue with
    | JsonValue.Array elements ->
        elements
        |> Array.filter (JsonRuntime.Matches cultureStr (InferedTypeTag.ParseCode tagCode))
        |> Array.mapi (fun i value -> doc.CreateNew(value, "[" + (string i) + "]") |> mapping.Invoke)
    | JsonValue.Null -> [| |]
    | x -> failwithf "Expecting an array at '%s', got %s" doc.Path <| x.ToString(JsonSaveOptions.DisableFormatting)

  /// Returns single or no value from an array matching the specified tag
  static member TryGetArrayChildByTypeTag<'T>(doc, cultureStr, tagCode, mapping:Func<IJsonDocument,'T>) = 
    match JsonRuntime.GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, mapping) with
    | [| child |] -> Some child
    | [| |] -> None
    | _ -> failwithf "Expecting an array with single or no elements at '%s', got %s" doc.Path <| doc.JsonValue.ToString(JsonSaveOptions.DisableFormatting)

  /// Returns a single array children that matches the specified tag
  static member GetArrayChildByTypeTag(doc, cultureStr, tagCode) = 
    match JsonRuntime.GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, Func<_,_>(id)) with
    | [| child |] -> child
    | _ -> failwithf "Expecting an array with single element at '%s', got %s" doc.Path <| doc.JsonValue.ToString(JsonSaveOptions.DisableFormatting)

  /// Returns a single or no value by tag type
  static member TryGetValueByTypeTag<'T>(doc:IJsonDocument, cultureStr, tagCode, mapping:Func<IJsonDocument,'T>) = 
    if JsonRuntime.Matches cultureStr (InferedTypeTag.ParseCode tagCode) doc.JsonValue
    then Some (mapping.Invoke doc)
    else None

  static member private ToJsonValue (cultureInfo:CultureInfo) (value:obj) = 
    let f g = function None -> JsonValue.Null | Some v -> g v
    if value = null then 
        JsonValue.Null
    elif value.GetType().IsArray then 
        JsonValue.Array [| for elem in unbox<Array> value -> JsonRuntime.ToJsonValue cultureInfo elem |]
    else
        match value with
        | :? string                  as v -> JsonValue.String v
        | :? option<string>          as v -> f JsonValue.String v
        | :? DateTime                as v -> v.ToString(cultureInfo) |> JsonValue.String
        | :? option<DateTime>        as v -> f (fun (dt:DateTime) -> dt.ToString(cultureInfo) |> JsonValue.String) v
        | :? int                     as v -> JsonValue.Number(decimal v)
        | :? option<int>             as v -> f (decimal >> JsonValue.Number) v
        | :? int64                   as v -> JsonValue.Number(decimal v)
        | :? option<int64>           as v -> f (decimal >> JsonValue.Number) v
        | :? float                   as v -> JsonValue.Number(decimal v)
        | :? option<float>           as v -> f (decimal >> JsonValue.Number) v
        | :? decimal                 as v -> JsonValue.Number v
        | :? option<decimal>         as v -> f JsonValue.Number v
        | :? bool                    as v -> JsonValue.Boolean v
        | :? option<bool>            as v -> f JsonValue.Boolean v
        | :? Guid                    as v -> v.ToString() |> JsonValue.String
        | :? option<Guid>            as v -> f (fun (g:Guid) -> g.ToString() |> JsonValue.String) v
        | :? IJsonDocument           as v -> v.JsonValue
        | :? option<IJsonDocument>   as v -> f (fun (v:IJsonDocument) -> v.JsonValue) v
        | :? JsonValue               as v -> v
        | :? option<JsonValue>       as v -> f id v
        | _ -> failwithf "Can't create JsonValue from %A" value

  // Creates a JsonValue and wraps it in a json document
  static member CreateValue(value:obj, cultureStr) = 
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json = JsonRuntime.ToJsonValue cultureInfo value
    JsonDocument.Create(json, "")

  // Creates a JsonValue.Record and wraps it in a json document
  static member CreateRecord(properties, cultureStr) =
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json = 
      properties 
      |> Array.map (fun (k, v:obj) -> k, JsonRuntime.ToJsonValue cultureInfo v)
      |> JsonValue.Record
    JsonDocument.Create(json, "")

  // Creates a scalar JsonValue.Array and wraps it in a json document
  static member CreateArray(elements:obj[], cultureStr) =
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json = 
      elements 
      |> Array.collect (JsonRuntime.ToJsonValue cultureInfo
                        >>
                        function JsonValue.Array elements -> elements | JsonValue.Null -> [| |] | element -> [| element |])
      |> JsonValue.Array
    JsonDocument.Create(json, "")
