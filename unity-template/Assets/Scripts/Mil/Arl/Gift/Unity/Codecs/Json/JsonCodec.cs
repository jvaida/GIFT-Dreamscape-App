using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class JsonCodec
{
    public abstract object Deserialize(JSONObject jObj);

    public abstract string Serialize(object obj);

    protected void CheckForExpectedFields(JSONObject jObj, params string[] fields)
    {
        if(jObj == null)
            throw new ArgumentNullException("jObj");
            
        foreach(var field in fields)
        {
            if(!jObj.HasField(field))
                throw new Exception("The JSON " 
                    + jObj.Print() 
                    + " was unable to be deserialized by the " 
                    + GetType().Name 
                    + " codec because the JSON does not contain the field " 
                    + field);
        }
    }

    protected object ParseFieldAsEnum(JSONObject jObj, string fieldName, Type type)
    {
        Debug.LogFormat("Parsing {0} in {1} as enum", fieldName, jObj.Print());
        
        //Argument validation
        if(jObj == null)
            throw new ArgumentNullException("jObj");
        
        //Parse the enum
        var field = jObj.GetField(fieldName);
        if(field.IsString && Enum.IsDefined(type, field.str))
            return Enum.Parse(type, field.str);
        else 
            throw new Exception(String.Format("The {0} field in {1} is not parsable as a {2} enum", 
                fieldName, 
                jObj.Print(), 
                type.Name));
    }

    protected string ParseFieldAsString(JSONObject jObj, string fieldName)
    {
        Debug.LogFormat("Parsing {0} in {1} as string", fieldName, jObj.Print());

        //Argument validation
        if(jObj == null)
            throw new ArgumentNullException("jObj");
        
        //Parse the string
        var field = jObj.GetField(fieldName);
        if(field.IsString)
            return field.str;
        else 
            throw new Exception(String.Format("The {0} field in {1} is not a string", 
                fieldName, 
                jObj.Print()));
    }

    protected Dictionary<string, string> ParseFieldAsDictionary(JSONObject jObj, string fieldName)
    {
        Debug.LogFormat("Parsing {0} in {1} as dictionary", fieldName, jObj.Print());

        //Argument validation
        if(jObj == null)
            throw new ArgumentNullException("jObj");
        
        //Parse the dictionary
        var field = jObj.GetField(fieldName);
        if(field.IsObject)
            return field.ToDictionary();
        else 
            throw new Exception(String.Format("The {0} field in {1} is not an object and therefore can't be converted to a Dictionary", 
                fieldName, 
                jObj.Print()));
    }
}